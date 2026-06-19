using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class MovementSystem : UpdateSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly List<int> _entities = new();
    private List<CollisionShape2D> _staticShapes = new();

    public void SetCollidersFromBoxes(List<BoundingBox2D> boxes)
    {
        _staticShapes = new List<CollisionShape2D>(boxes.Count);
        foreach (var box in boxes)
            _staticShapes.Add(new CollisionShape2D(box));
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

            var size = body.HalfExtents * 2f;

            pos.Value += new Vector2(vel.Value.X * dt, 0);
            ResolveHorizontal(pos, vel, size);

            grounded.Value = false;
            float oldVelY = vel.Value.Y;
            pos.Value += new Vector2(0, vel.Value.Y * dt);
            ResolveVertical(pos, vel, size, grounded, oldVelY);

            if (entity.Has<CollisionActor>())
            {
                var actor = entity.Get<CollisionActor>();
                var box = BoundingBox2D.CreateFromPositionAndSize(pos.Value, size);
                actor.Shape = new CollisionShape2D(box);
            }
        }
    }

    private static CollisionShape2D MakeShape(Vector2 pos, Vector2 size)
    {
        return new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(pos, size));
    }

    private void ResolveHorizontal(Position pos, Velocity vel, Vector2 size)
    {
        var shape = MakeShape(pos.Value, size);

        foreach (var staticShape in _staticShapes)
        {
            if (!shape.TryGetCollision(staticShape, out var result))
                continue;

            if (result.MinimumTranslationVector.X == 0)
                continue;

            pos.Value += new Vector2(result.MinimumTranslationVector.X, 0);
            vel.Value = new Vector2(0, vel.Value.Y);
            shape = MakeShape(pos.Value, size);
        }
    }

    private void ResolveVertical(Position pos, Velocity vel, Vector2 size, Grounded grounded, float oldVelY)
    {
        var shape = MakeShape(pos.Value, size);

        foreach (var staticShape in _staticShapes)
        {
            if (!shape.TryGetCollision(staticShape, out var result))
                continue;

            if (result.MinimumTranslationVector.Y == 0)
                continue;

            pos.Value += new Vector2(0, result.MinimumTranslationVector.Y);
            vel.Value = new Vector2(vel.Value.X, 0);

            if (result.MinimumTranslationVector.Y < 0 && oldVelY > 0)
                grounded.Value = true;

            shape = MakeShape(pos.Value, size);
        }
    }
}
