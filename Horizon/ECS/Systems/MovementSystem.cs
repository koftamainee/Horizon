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
    private readonly Dictionary<int, CollisionShape2D> _staticShapes;
    private readonly HashSet<int> _oneWayIds;

    public MovementSystem(CollisionWorld2D collisionWorld, Dictionary<int, CollisionShape2D> staticShapes, HashSet<int> oneWayIds)
    {
        _collisionWorld = collisionWorld;
        _staticShapes = staticShapes;
        _oneWayIds = oneWayIds;
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

                var hit = false;
                foreach (var pair in _collisionWorld.QueryCollisionPairs("dynamic", "static"))
                {
                    if (pair.FirstId != id) continue;

                    var staticId = pair.SecondId;
                    if (!_staticShapes.TryGetValue(staticId, out var staticShape))
                        continue;

                    if (!shape.TryGetCollision(staticShape, out var result))
                        continue;

                    var mtv = result.MinimumTranslationVector;
                    if (mtv.LengthSquared() < 0.0001f)
                        continue;

                    if (_oneWayIds.Contains(staticId))
                    {
                        var oneWayNormal = mtv.LengthSquared() > 0 ? Vector2.Normalize(mtv) : Vector2.Zero;
                        if (oneWayNormal.Y > -0.01f)
                            continue;
                    }

                    pos.Value += mtv;
                    shape = MakeShape(pos.Value, size);

                    var mtvLen = mtv.Length();
                    if (mtvLen > 0.0001f)
                    {
                        var normal = mtv / mtvLen;
                        var vDotN = Vector2.Dot(vel.Value, normal);
                        if (vDotN < 0)
                            vel.Value -= vDotN * normal;
                    }

                    if (mtv.Y < 0 && vel.Value.Y <= 0)
                        grounded.Value = true;

                    hit = true;
                    break;
                }

                if (!hit) break;
            }

            actor.Shape = MakeShape(pos.Value, size);
        }
    }

    private static CollisionShape2D MakeShape(Vector2 pos, Vector2 size)
    {
        return new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(pos, size));
    }
}
