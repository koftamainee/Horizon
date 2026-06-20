using System;
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
using MonoGame.Extended.Tilemaps;
using MonoGame.Extended.Tilemaps.Rendering;
using MonoGame.Extended.ViewportAdapters;

namespace Horizon.Screens;

public sealed class GameplayScreen : GameScreen
{
    private new Game1 Game => (Game1)base.Game;

    private readonly MonoGame.Extended.ECS.World _world;
    private readonly OrthographicCamera _camera;
    private readonly RoomManager _roomManager;
    private readonly PlayerControlSystem _playerControl;
    private readonly CameraFollowSystem _cameraFollow;
    private readonly CollisionWorld2D _collisionWorld;
    private readonly MovementSystem _movement;
    private readonly TilemapSpriteBatchRenderer _tilemapRenderer;
    private readonly Texture2D _pixelTexture;
    private int _playerEntityId;
    private bool _showDebugColliders;
    private KeyboardState _prevKeyboardState;
    private bool _prevTransitioning;

    public GameplayScreen(Game1 game) : base(game)
    {
        _pixelTexture = CreateTexture(GraphicsDevice);

        var viewportAdapter = new DefaultViewportAdapter(GraphicsDevice);
        _camera = new OrthographicCamera(viewportAdapter);

        _collisionWorld = new CollisionWorld2D();
        _collisionWorld.AddLayer("dynamic", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.AddLayer("static", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.AddLayer("hazard", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "static");
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "hazard");

        _tilemapRenderer = new TilemapSpriteBatchRenderer();
        _cameraFollow = new CameraFollowSystem(_camera, GraphicsDevice);

        _playerControl = new PlayerControlSystem(Game.InputSystem);
        var gravity = new GravitySystem();
        _movement = new MovementSystem(_collisionWorld);
        var render = new RenderSystem(GraphicsDevice, _camera);

        _world = new WorldBuilder()
            .AddSystem(_playerControl)
            .AddSystem(gravity)
            .AddSystem(_movement)
            .AddSystem(_cameraFollow)
            .AddSystem(render)
            .Build();

        // Create player entity
        var player = _world.CreateEntity();
        _playerEntityId = player.Id;
        player.Attach(new Position());
        player.Attach(new Velocity());
        player.Attach(new Gravity());
        player.Attach(new Grounded());
        player.Attach(new PlayerTag());
        player.Attach(new Body { HalfExtents = new Vector2(16, 16) });

        var size = new Vector2(32, 32);
        var playerActor = new CollisionActor
        {
            Id = player.Id,
            Shape = new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(Vector2.Zero, size))
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

        var bounds = BoundingBox2D.CreateFromPositionAndSize(Vector2.Zero, new Vector2(1600, 900));
        _roomManager = new RoomManager(Game.Content, _world, _collisionWorld,
            _camera, _tilemapRenderer, bounds);
        _roomManager.SetPlayer(_playerEntityId);

        _roomManager.EntityFactory.Register("enemy", obj =>
        {
            var enemy = _world.CreateEntity();
            var pos = obj.Position;
            var rawSize = obj is TilemapRectangleObject rectObj ? rectObj.Size : new Vector2(32, 32);
            var sz = new Vector2(Math.Max(rawSize.X, 8), Math.Max(rawSize.Y, 8));

            enemy.Attach(new Position { Value = pos });
            enemy.Attach(new Body { HalfExtents = sz / 2f });

            var enemyActor = new CollisionActor
            {
                Id = enemy.Id,
                Shape = new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(pos, sz))
            };
            enemy.Attach(enemyActor);
            _collisionWorld.Insert(enemyActor, "dynamic");

            var tex = new Texture2D(GraphicsDevice, (int)sz.X, (int)sz.Y);
            var px = new Color[(int)(sz.X * sz.Y)];
            Array.Fill(px, Color.Orange);
            tex.SetData(px);
            enemy.Attach(new SpriteRenderer
            {
                Texture = tex,
                Source = new Rectangle(0, 0, (int)sz.X, (int)sz.Y),
                Tint = Color.Orange
            });

            return enemy.Id;
        });

        _roomManager.LoadInitialRoom("rooms/test_room/test_room", new Vector2(1278, 169));

        var playerPos = player.Get<Position>().Value;
        _camera.Position = playerPos - _camera.Origin;
        _cameraFollow.SetBounds(_roomManager.CameraBounds);
    }

    public override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        if (kb.IsKeyDown(Keys.F3) && _prevKeyboardState.IsKeyUp(Keys.F3))
            _showDebugColliders = !_showDebugColliders;
        _prevKeyboardState = kb;

        _roomManager.Update(gameTime);

        if (_prevTransitioning && !_roomManager.IsTransitioning)
            _cameraFollow.SetBounds(_roomManager.CameraBounds);
        _prevTransitioning = _roomManager.IsTransitioning;

        _world.Update(gameTime);
        _tilemapRenderer.Update(gameTime);

        if (_movement.HadHazardCollisionThisFrame)
        {
            _roomManager.RespawnPlayer();
            return;
        }

        CheckTriggerZones();
        CheckCheckpoints();
    }

    private void CheckTriggerZones()
    {
        if (_roomManager.IsTransitioning) return;

        var player = _world.GetEntity(_playerEntityId);
        if (player == null) return;

        var pos = player.Get<Position>().Value;
        var halfExt = player.Get<Body>().HalfExtents;
        var playerBounds = BoundingBox2D.CreateFromPositionAndSize(
            pos, halfExt * 2f);

        foreach (var trigger in _roomManager.CurrentRoom.Triggers)
        {
            if (!playerBounds.Intersects(trigger.Bounds)) continue;

            if (trigger.Type == "room_transition" && !string.IsNullOrEmpty(trigger.TargetRoom))
            {
                _roomManager.SwitchTo(trigger.TargetRoom, trigger.TargetSpawn);
                return;
            }
        }
    }

    private void CheckCheckpoints()
    {
        if (_roomManager.IsTransitioning) return;

        var player = _world.GetEntity(_playerEntityId);
        if (player == null) return;

        var pos = player.Get<Position>().Value;
        var halfExt = player.Get<Body>().HalfExtents;
        var playerBounds = BoundingBox2D.CreateFromPositionAndSize(
            pos, halfExt * 2f);

        foreach (var bounds in _roomManager.CurrentRoom.SpawnBounds)
        {
            if (playerBounds.Intersects(bounds))
            {
                _roomManager.ApplyCheckpoint(bounds.Center);
                return;
            }
        }
    }

    public override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        var sb = Game.SpriteBatch;

        if (_roomManager.CurrentRoom.Tilemap != null)
        {
            _tilemapRenderer.Draw(sb, _camera);
        }

        _world.Draw(gameTime);

        if (_showDebugColliders)
        {
            var room = _roomManager.CurrentRoom;
            sb.Begin(transformMatrix: _camera.GetViewMatrix());

            // Static colliders (Collision layer + Hazard layer)
            for (int i = 0; i < room.ColliderShapes.Count; i++)
            {
                var isHazard = room.HazardIds.Contains(i);
                var color = isHazard ? Color.Red : Color.Lime;
                var verts = i < room.PolygonVertices.Count ? room.PolygonVertices[i] : null;
                if (verts != null)
                {
                    for (int j = 0; j < verts.Length; j++)
                    {
                        var a = verts[j];
                        var b = verts[(j + 1) % verts.Length];
                        sb.DrawLine(a, b, color);
                    }
                }
                else
                {
                    var box = room.ColliderShapes[i].BoundingBox;
                    var x = (int)box.Min.X;
                    var y = (int)box.Min.Y;
                    var w = (int)box.Width;
                    var h = (int)box.Height;
                    sb.Draw(_pixelTexture, new Rectangle(x, y, w, 1), color);
                    sb.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), color);
                    sb.Draw(_pixelTexture, new Rectangle(x, y, 1, h), color);
                    sb.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), color);
                }
            }

            // Triggers (Triggers layer) — blue
            foreach (var trigger in room.Triggers)
            {
                var x = (int)trigger.Bounds.Min.X;
                var y = (int)trigger.Bounds.Min.Y;
                var w = (int)trigger.Bounds.Width;
                var h = (int)trigger.Bounds.Height;
                sb.Draw(_pixelTexture, new Rectangle(x, y, w, 1), Color.CornflowerBlue);
                sb.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), Color.CornflowerBlue);
                sb.Draw(_pixelTexture, new Rectangle(x, y, 1, h), Color.CornflowerBlue);
                sb.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), Color.CornflowerBlue);
            }

            // Spawn points (Spawns layer) — yellow rectangles
            foreach (var bounds in room.SpawnBounds)
            {
                var x = (int)bounds.Min.X;
                var y = (int)bounds.Min.Y;
                var w = (int)bounds.Width;
                var h = (int)bounds.Height;
                sb.Draw(_pixelTexture, new Rectangle(x, y, w, 1), Color.Yellow);
                sb.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), Color.Yellow);
                sb.Draw(_pixelTexture, new Rectangle(x, y, 1, h), Color.Yellow);
                sb.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), Color.Yellow);
            }

            // Entity objects (Entities layer) — orange outline
            foreach (var obj in room.EntityObjects)
            {
                if (obj is TilemapRectangleObject rectObj)
                {
                    var x = (int)obj.Position.X;
                    var y = (int)obj.Position.Y;
                    var w = (int)rectObj.Size.X;
                    var h = (int)rectObj.Size.Y;
                    sb.Draw(_pixelTexture, new Rectangle(x, y, w, 1), Color.Orange);
                    sb.Draw(_pixelTexture, new Rectangle(x, y + h - 1, w, 1), Color.Orange);
                    sb.Draw(_pixelTexture, new Rectangle(x, y, 1, h), Color.Orange);
                    sb.Draw(_pixelTexture, new Rectangle(x + w - 1, y, 1, h), Color.Orange);
                }
                else
                {
                    var px = (int)obj.Position.X;
                    var py = (int)obj.Position.Y;
                    sb.Draw(_pixelTexture, new Rectangle(px - 2, py, 5, 1), Color.Orange);
                    sb.Draw(_pixelTexture, new Rectangle(px, py - 2, 1, 5), Color.Orange);
                }
            }

            sb.End();
        }

        if (_roomManager.FadeAlpha > 0f)
        {
            var vp = GraphicsDevice.Viewport;
            sb.Begin(transformMatrix: null);
            sb.Draw(_pixelTexture, new Rectangle(0, 0, vp.Width, vp.Height),
                Color.Black * _roomManager.FadeAlpha);
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
