using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class RenderSystem : DrawSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly SpriteBatch _spriteBatch;
    private readonly OrthographicCamera _camera;
    private readonly List<int> _entities = new();

    public RenderSystem(GraphicsDevice graphicsDevice, OrthographicCamera camera)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _camera = camera;
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
        if (entity != null && entity.Has<Position>() && entity.Has<SpriteRenderer>())
            _entities.Add(entityId);
    }

    private void OnEntityRemoved(int entityId)
    {
        _entities.Remove(entityId);
    }

    public override void Draw(GameTime gameTime)
    {
        _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());

        foreach (int id in _entities)
        {
            var entity = _world.GetEntity(id);
            if (entity == null) continue;

            var pos = entity.Get<Position>();
            var sprite = entity.Get<SpriteRenderer>();

            _spriteBatch.Draw(sprite.Texture, pos.Value, sprite.Source, sprite.Tint);
        }

        _spriteBatch.End();
    }
}
