using System;
using System.Collections.Generic;
using System.IO;
using Horizon.Core;
using Horizon.ECS.Components;
using Horizon.ECS.Systems;
using Horizon.World;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.ECS;
using MonoGame.Extended.Screens;
using MonoGame.Extended.Tilemaps.Rendering;
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
    private readonly TilemapSpriteBatchRenderer _tilemapRenderer;
    private readonly Texture2D _pixelTexture;
    private bool _showDebugColliders;
    private KeyboardState _prevKeyboardState;

    public GameplayScreen(Game1 game) : base(game)
    {
        _pixelTexture = CreateTexture(GraphicsDevice);

        var tmxPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "Content", "rooms", "test_room", "test_room.tmx");

        _room = Room.LoadFromTiled(tmxPath, GraphicsDevice);

        _viewportAdapter = new DefaultViewportAdapter(GraphicsDevice);
        _camera = new OrthographicCamera(_viewportAdapter);

        _collisionWorld = new CollisionWorld2D();
        _collisionWorld.AddLayer("dynamic", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.AddLayer("static", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "static");

        var staticShapes = new Dictionary<int, CollisionShape2D>();
        var oneWayIds = new HashSet<int>();
        int nextStaticId = 1;
        int colliderIndex = 0;

        foreach (var box in _room.Colliders)
        {
            var staticActor = new CollisionActor
            {
                Id = nextStaticId,
                Shape = new CollisionShape2D(box),
            };
            _collisionWorld.Insert(staticActor, "static");
            staticShapes[nextStaticId] = staticActor.Shape;
            if (_room.OneWayIds.Contains(colliderIndex))
                oneWayIds.Add(nextStaticId);
            nextStaticId++;
            colliderIndex++;
        }

        _playerControl = new PlayerControlSystem(Game.InputSystem);
        var gravity = new GravitySystem();
        var movement = new MovementSystem(_collisionWorld, staticShapes, oneWayIds);
        var damage = new DamageSystem(_collisionWorld);
        var cameraFollow = new CameraFollowSystem(_camera, GraphicsDevice);
        cameraFollow.SetBounds(_room.Bounds);
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
        var startPos = _room.SpawnPoints.TryGetValue("player_start", out var spawn)
            ? spawn
            : new Vector2(200, 100);
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

        _tilemapRenderer = new TilemapSpriteBatchRenderer();
        if (_room.Tilemap != null)
            _tilemapRenderer.LoadTilemap(_room.Tilemap);

        var entityFactory = new EntityFactory();
        foreach (var obj in _room.EntityObjects)
            entityFactory.Create(obj);
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.F3) && _prevKeyboardState.IsKeyUp(Keys.F3))
            _showDebugColliders = !_showDebugColliders;
        _prevKeyboardState = kb;

        _world.Update(gameTime);
        _tilemapRenderer.Update(gameTime);
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        var sb = Game.SpriteBatch;

        if (_room.Tilemap != null)
        {
            _tilemapRenderer.Draw(sb, _camera);
        }

        _world.Draw(gameTime);

        if (_showDebugColliders)
        {
            sb.Begin(transformMatrix: _camera.GetViewMatrix());
            foreach (var collider in _room.Colliders)
            {
                var x = (int)collider.Min.X;
                var y = (int)collider.Min.Y;
                var w = (int)collider.Width;
                var h = (int)collider.Height;
                sb.Draw(_pixelTexture, new Rectangle(x, y, w, 1), Color.Lime);
                sb.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), Color.Lime);
                sb.Draw(_pixelTexture, new Rectangle(x, y, 1, h), Color.Lime);
                sb.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), Color.Lime);
            }
            sb.End();
        }
    }

    private static Texture2D CreateTexture(GraphicsDevice gd)
    {
        var tex = new Texture2D(gd, 1, 1);
        tex.SetData(new[] { Color.White });
        return tex;
    }
}
