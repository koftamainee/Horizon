using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class DamageSystem : UpdateSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly CollisionWorld2D _collisionWorld;
    private readonly List<int> _entities = new();

    public DamageSystem(CollisionWorld2D collisionWorld)
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
        if (entity != null && entity.Has<CollisionActor>())
            _entities.Add(entityId);
    }

    private void OnEntityRemoved(int entityId)
    {
        _entities.Remove(entityId);
    }

    public override void Update(GameTime gameTime)
    {
        _collisionWorld.RebuildDynamicLayers();

        foreach (var pair in _collisionWorld.QueryCollisionPairs("dynamic", "dynamic"))
        {
            var firstEntity = _world.GetEntity(pair.FirstId);
            var secondEntity = _world.GetEntity(pair.SecondId);

            if (firstEntity == null || secondEntity == null)
                continue;

            bool firstIsPlayer = firstEntity.Has<PlayerTag>();
            bool secondIsPlayer = secondEntity.Has<PlayerTag>();

            if (!firstIsPlayer && !secondIsPlayer)
                continue;

            // Contact damage placeholder:
            // When player touches an enemy (entity without PlayerTag → treat as enemy),
            // apply damage. For now just detected; Health component TBD.
        }
    }
}
