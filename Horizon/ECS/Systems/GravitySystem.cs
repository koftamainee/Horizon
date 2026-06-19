using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class GravitySystem : UpdateSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly List<int> _entities = new();

    public override void Initialize(MonoGame.Extended.ECS.World world)
    {
        _world = world;
        world.EntityAdded += OnEntityAdded;
        world.EntityRemoved += OnEntityRemoved;
    }

    private void OnEntityAdded(int entityId)
    {
        var entity = _world.GetEntity(entityId);
        if (entity != null && entity.Has<Velocity>() && entity.Has<Gravity>())
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

            var vel = entity.Get<Velocity>();
            var grav = entity.Get<Gravity>();

            float multiplier = vel.Value.Y > 0 ? grav.FallMultiplier : 1f;
            vel.Value.Y += grav.Strength * multiplier * dt;
        }
    }
}
