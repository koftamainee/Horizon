using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class MovementSystem : UpdateSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly List<int> _entities = new();
    private readonly CollisionWorld2D _collisionWorld;

    public bool HadHazardCollisionThisFrame { get; private set; }

    public MovementSystem(CollisionWorld2D collisionWorld)
    {
        _collisionWorld = collisionWorld;
    }

    public override void Initialize(MonoGame.Extended.ECS.World world)
    {
        _world = world;
        world.EntityAdded += OnEntityAdded;
        world.EntityRemoved += OnEntityRemoved;
    }

    private void OnEntityAdded(int entityId)
    {
        var entity = _world.GetEntity(entityId);
        if (entity != null && entity.Has<Position>() && entity.Has<Velocity>() && entity.Has<Body>() && entity.Has<Grounded>())
            _entities.Add(entityId);
    }

    private void OnEntityRemoved(int entityId)
    {
        _entities.Remove(entityId);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        HadHazardCollisionThisFrame = false;

        foreach (int id in _entities)
        {
            var entity = _world.GetEntity(id);
            if (entity == null) continue;

            var pos = entity.Get<Position>();
            var vel = entity.Get<Velocity>();
            var body = entity.Get<Body>();
            var grounded = entity.Get<Grounded>();
            var actor = entity.Get<CollisionActor>();

            grounded.Value = false;
            var size = body.HalfExtents * 2f;

            pos.Value += vel.Value * dt;

            for (int iter = 0; iter < 4; iter++)
            {
                var shape = MakeShape(pos.Value, size);
                actor.Shape = shape;

                _collisionWorld.RebuildDynamicLayers();

                var hits = new List<CollisionHit>();

                void GatherHits(string layer)
                {
                    foreach (var pair in _collisionWorld.QueryCollisionPairs("dynamic", layer))
                    {
                        if (pair.FirstId != id) continue;

                        ICollisionActor staticActor = pair.Second;
                        var staticShape = staticActor.Shape;

                        if (!shape.TryGetCollision(staticShape, out var result))
                            continue;

                        var mtv = result.MinimumTranslationVector;
                        if (mtv.LengthSquared() < 0.0001f)
                            continue;

                        bool isOneWay = false;
                        bool isHazard = false;
                        if (staticActor is CollisionActor ca)
                        {
                            isOneWay = ca.IsOneWay;
                            isHazard = ca.IsHazard;
                        }

                        hits.Add(new CollisionHit
                        {
                            MTV = mtv,
                            StaticActor = staticActor,
                            IsOneWay = isOneWay,
                            IsHazard = isHazard,
                        });
                    }
                }

                GatherHits("static");
                GatherHits("hazard");

                if (hits.Count == 0) break;

                hits.Sort((a, b) => b.MTV.LengthSquared().CompareTo(a.MTV.LengthSquared()));

                bool anyResolved = false;
                foreach (var hit in hits)
                {
                    var mtv = hit.MTV;
                    var mtvLen = mtv.Length();
                    var normal = mtvLen > 0.0001f ? mtv / mtvLen : Vector2.Zero;

                    if (hit.IsOneWay)
                    {
                        if (normal.Y > -0.7f)
                            continue;
                    }

                    pos.Value += mtv;
                    shape = MakeShape(pos.Value, size);

                    var vDotN = Vector2.Dot(vel.Value, normal);
                    if (vDotN < 0)
                        vel.Value -= vDotN * normal;

                    if (normal.Y < -0.7f && vel.Value.Y <= 0)
                        grounded.Value = true;

                    if (hit.IsHazard)
                        HadHazardCollisionThisFrame = true;

                    anyResolved = true;
                }

                if (!anyResolved) break;
            }

            actor.Shape = MakeShape(pos.Value, size);
        }
    }

    private static CollisionShape2D MakeShape(Vector2 pos, Vector2 size)
    {
        return new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(pos, size));
    }

    private struct CollisionHit
    {
        public Vector2 MTV;
        public ICollisionActor StaticActor;
        public bool IsOneWay;
        public bool IsHazard;
    }
}
