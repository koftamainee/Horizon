using System;
using Horizon.Core;
using Horizon.ECS.Components;
using Horizon.ECS.Systems;
using Horizon.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.ECS;
using MonoGame.Extended.Screens;
using MonoGame.Extended.ViewportAdapters;

namespace Horizon.Screens;

public sealed class GameplayScreen : GameScreen
{
    private new Game1 Game => (Game1)base.Game;

    private readonly MonoGame.Extended.ECS.World _world;
    private readonly OrthographicCamera _camera;
    private readonly DefaultViewportAdapter _viewportAdapter;
    private readonly Room _room;
    private readonly PlayerControlSystem _playerControl;
    private readonly CollisionWorld2D _collisionWorld;
    private readonly Texture2D _pixelTexture;

    public GameplayScreen(Game1 game) : base(game)
    {
        _room = new Room();
        _pixelTexture = CreateTexture(GraphicsDevice);

        _viewportAdapter = new DefaultViewportAdapter(GraphicsDevice);
        _camera = new OrthographicCamera(_viewportAdapter);

        _collisionWorld = new CollisionWorld2D();
        _collisionWorld.AddLayer("dynamic", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "dynamic");

        _playerControl = new PlayerControlSystem(Game.InputSystem);
        var gravity = new GravitySystem();
        var movement = new MovementSystem();
        movement.SetCollidersFromBoxes(_room.Colliders);
        var damage = new DamageSystem(_collisionWorld);
        var cameraFollow = new CameraFollowSystem(_camera, GraphicsDevice);
        var render = new RenderSystem(GraphicsDevice, _camera);

        _world = new WorldBuilder()
            .AddSystem(_playerControl)
            .AddSystem(gravity)
            .AddSystem(movement)
            .AddSystem(damage)
            .AddSystem(cameraFollow)
            .AddSystem(render)
            .Build();

        var player = _world.CreateEntity();
        var startPos = new Vector2(200, 100);
        player.Attach(new Position { Value = startPos });
        player.Attach(new Velocity());
        player.Attach(new Gravity());
        player.Attach(new Grounded());
        player.Attach(new PlayerTag());
        player.Attach(new Body { HalfExtents = new Vector2(16, 16) });

        var size = new Vector2(32, 32);
        var playerActor = new CollisionActor
        {
            Id = player.Id,
            Shape = new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(startPos, size))
        };
        player.Attach(playerActor);
        _collisionWorld.Insert(playerActor, "dynamic");

        var texture = new Texture2D(GraphicsDevice, 32, 32);
        var pixels = new Color[32 * 32];
        Array.Fill(pixels, Color.White);
        texture.SetData(pixels);
        player.Attach(new SpriteRenderer
        {
            Texture = texture,
            Source = new Rectangle(0, 0, 32, 32),
            Tint = Color.Red
        });

        _camera.Position = startPos - _camera.Origin;
    }

    public override void Update(GameTime gameTime)
    {
        _world.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _world.Draw(gameTime);

        var sb = Game.SpriteBatch;
        sb.Begin(transformMatrix: _camera.GetViewMatrix());
        foreach (var collider in _room.Colliders)
        {
            var rect = new Rectangle(
                (int)collider.Min.X, (int)collider.Min.Y,
                (int)collider.Width, (int)collider.Height);
            sb.Draw(_pixelTexture, rect, Color.Gray);
        }
        sb.End();
    }

    private static Texture2D CreateTexture(GraphicsDevice gd)
    {
        var tex = new Texture2D(gd, 1, 1);
        tex.SetData(new[] { Color.White });
        return tex;
    }
}
