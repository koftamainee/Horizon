using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class CameraFollowSystem : UpdateSystem
{
    private MonoGame.Extended.ECS.World _world;
    private readonly OrthographicCamera _camera;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly List<int> _players = new();
    private BoundingBox2D _bounds;

    private const float CameraSmoothness = 8f;
    private const float DeadZoneMargin = 0.4f;

    public CameraFollowSystem(OrthographicCamera camera, GraphicsDevice graphicsDevice)
    {
        _camera = camera;
        _graphicsDevice = graphicsDevice;
        _bounds = BoundingBox2D.CreateFromPositionAndSize(Vector2.Zero, new Vector2(1600, 900));
    }

    public void SetBounds(BoundingBox2D bounds)
    {
        _bounds = bounds;
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
        if (entity != null && entity.Has<PlayerTag>() && entity.Has<Position>())
            _players.Add(entityId);
    }

    private void OnEntityRemoved(int entityId)
    {
        _players.Remove(entityId);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        foreach (int id in _players)
        {
            var entity = _world.GetEntity(id);
            if (entity == null) continue;

            var pos = entity.Get<Position>();
            FollowPlayer(pos.Value, dt);
        }
    }

    private void FollowPlayer(Vector2 targetPos, float dt)
    {
        var camPos = _camera.Position;
        var screenPos = targetPos - camPos;

        float viewW = _graphicsDevice.Viewport.Width;
        float viewH = _graphicsDevice.Viewport.Height;

        float left = viewW * DeadZoneMargin;
        float top = viewH * DeadZoneMargin;
        float right = viewW * (1f - DeadZoneMargin);
        float bottom = viewH * (1f - DeadZoneMargin);

        var desiredPos = camPos;

        if (screenPos.X < left)
            desiredPos.X = targetPos.X - left;

        if (screenPos.X > right)
            desiredPos.X = targetPos.X - right;

        if (screenPos.Y < top)
            desiredPos.Y = targetPos.Y - top;

        if (screenPos.Y > bottom)
            desiredPos.Y = targetPos.Y - bottom;

        float t = MathHelper.Clamp(CameraSmoothness * dt, 0f, 1f);
        var smoothPos = Vector2.Lerp(camPos, desiredPos, t);

        var viewSize = new Vector2(_graphicsDevice.Viewport.Width, _graphicsDevice.Viewport.Height);

        if (_bounds.Width > viewSize.X)
            smoothPos.X = MathHelper.Clamp(smoothPos.X,
                _bounds.Min.X + viewSize.X / 2f,
                _bounds.Max.X - viewSize.X / 2f);

        if (_bounds.Height > viewSize.Y)
            smoothPos.Y = MathHelper.Clamp(smoothPos.Y,
                _bounds.Min.Y + viewSize.Y / 2f,
                _bounds.Max.Y - viewSize.Y / 2f);

        _camera.Position = smoothPos;
    }
}
