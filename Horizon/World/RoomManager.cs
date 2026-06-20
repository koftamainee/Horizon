using System.Collections.Generic;
using Horizon.ECS.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using MonoGame.Extended;
using MonoGame.Extended.Collisions;
using MonoGame.Extended.Collisions.Layers;
using MonoGame.Extended.Tilemaps.Rendering;

namespace Horizon.World;

public class RoomManager
{
    private readonly ContentManager _content;
    private readonly MonoGame.Extended.ECS.World _world;
    private readonly CollisionWorld2D _collisionWorld;
    private readonly OrthographicCamera _camera;
    private readonly TilemapSpriteBatchRenderer _renderer;
    private readonly List<int> _roomScopedEntities = new();
    private readonly SessionState _sessionState = new();

    private Room _currentRoom;
    private int _playerEntityId = -1;
    private BoundingBox2D _cameraBounds;

    private enum FadeState { None, FadingOut, FadingIn }

    private FadeState _fadeState = FadeState.None;
    private float _fadeTimer;
    private float _fadeDuration = 0.3f;
    private string _targetRoomId;
    private string _targetSpawnName;

    public Room CurrentRoom => _currentRoom;
    public BoundingBox2D CameraBounds => _cameraBounds;
    public float FadeAlpha { get; private set; }
    public EntityFactory EntityFactory { get; } = new();
    public bool IsTransitioning => _fadeState != FadeState.None;

    public RoomManager(ContentManager content, MonoGame.Extended.ECS.World world,
        CollisionWorld2D collisionWorld, OrthographicCamera camera,
        TilemapSpriteBatchRenderer renderer, BoundingBox2D initialCameraBounds)
    {
        _content = content;
        _world = world;
        _collisionWorld = collisionWorld;
        _camera = camera;
        _renderer = renderer;
        _cameraBounds = initialCameraBounds;
    }

    public void SetPlayer(int entityId)
    {
        _playerEntityId = entityId;
    }

    public void LoadInitialRoom(string roomId, Vector2 startPos)
    {
        _currentRoom = Room.LoadFromTiled(roomId, _content);
        _cameraBounds = _currentRoom.Bounds;

        RestoreSessionState();

        _currentRoom.Visit.RespawnPosition = startPos;

        BuildColliders();
        if (_currentRoom.Tilemap != null)
            _renderer.LoadTilemap(_currentRoom.Tilemap);
        SpawnRoomEntities();

        PlacePlayerAt(startPos);
    }

    public void SwitchTo(string roomId, string spawnName = null)
    {
        if (_fadeState != FadeState.None) return;
        _targetRoomId = roomId;
        _targetSpawnName = spawnName;
        _fadeState = FadeState.FadingOut;
        _fadeTimer = 0f;
    }

    public void RespawnPlayer()
    {
        if (_playerEntityId < 0) return;
        var entity = _world.GetEntity(_playerEntityId);
        if (entity == null) return;

        PlacePlayerAt(_currentRoom.Visit.RespawnPosition);
    }

    public void Update(GameTime gameTime)
    {
        if (_fadeState == FadeState.None) return;

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _fadeTimer += dt;

        switch (_fadeState)
        {
            case FadeState.FadingOut:
                FadeAlpha = MathHelper.Clamp(_fadeTimer / _fadeDuration, 0f, 1f);
                if (_fadeTimer >= _fadeDuration)
                {
                    FadeAlpha = 1f;
                    PerformTransition();
                    _fadeState = FadeState.FadingIn;
                    _fadeTimer = 0f;
                }
                break;

            case FadeState.FadingIn:
                FadeAlpha = MathHelper.Clamp(1f - _fadeTimer / _fadeDuration, 0f, 1f);
                if (_fadeTimer >= _fadeDuration)
                {
                    FadeAlpha = 0f;
                    _fadeState = FadeState.None;
                }
                break;
        }
    }

    private void PerformTransition()
    {
        UnloadCurrentRoom();
        LoadNewRoom(_targetRoomId, _targetSpawnName);
    }

    private void UnloadCurrentRoom()
    {
        _collisionWorld.RemoveLayer("static");
        _collisionWorld.AddLayer("static", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "static");

        _collisionWorld.RemoveLayer("hazard");
        _collisionWorld.AddLayer("hazard", new Layer(new SpatialHash(new SizeF(64, 64))));
        _collisionWorld.EnableCollisionBetweenLayers("dynamic", "hazard");

        foreach (int id in _roomScopedEntities)
        {
            var entity = _world.GetEntity(id);
            if (entity != null)
            {
                if (entity.Has<CollisionActor>())
                {
                    var actor = entity.Get<CollisionActor>();
                    if (_collisionWorld.Contains(actor))
                        _collisionWorld.Remove(actor);
                }
                _world.DestroyEntity(id);
            }
        }
        _roomScopedEntities.Clear();
    }

    private void SaveSessionState()
    {
        if (_currentRoom != null)
            _sessionState.Rooms[_currentRoom.Id] = _currentRoom.State;
    }

    private void RestoreSessionState()
    {
        if (_sessionState.Rooms.TryGetValue(_currentRoom.Id, out var savedState))
            _currentRoom.State = savedState;
    }

    private void LoadNewRoom(string roomId, string spawnName)
    {
        SaveSessionState();

        _currentRoom = Room.LoadFromTiled(roomId, _content);
        _cameraBounds = _currentRoom.Bounds;

        RestoreSessionState();

        Vector2 spawnPos;
        if (!string.IsNullOrEmpty(spawnName) && _currentRoom.NamedSpawns.TryGetValue(spawnName, out var sp))
            spawnPos = sp;
        else if (_currentRoom.SpawnBounds.Count > 0)
            spawnPos = _currentRoom.SpawnBounds[0].Center;
        else
            spawnPos = Vector2.Zero;
        _currentRoom.Visit.RespawnPosition = spawnPos;

        BuildColliders();
        if (_currentRoom.Tilemap != null)
            _renderer.LoadTilemap(_currentRoom.Tilemap);
        SpawnRoomEntities();

        PlacePlayerAt(spawnPos);
    }

    private void BuildColliders()
    {
        int nextId = 1;
        int colliderIndex = 0;
        foreach (var shape in _currentRoom.ColliderShapes)
        {
            bool isHazard = _currentRoom.HazardIds.Contains(colliderIndex);
            var actor = new CollisionActor
            {
                Id = nextId,
                Shape = shape,
                IsOneWay = _currentRoom.OneWayIds.Contains(colliderIndex),
                IsHazard = isHazard,
            };
            _collisionWorld.Insert(actor, isHazard ? "hazard" : "static");
            nextId++;
            colliderIndex++;
        }
    }

    private void SpawnRoomEntities()
    {
        foreach (var obj in _currentRoom.EntityObjects)
        {
            int? entityId = EntityFactory.Create(obj);
            if (entityId.HasValue)
            {
                var entity = _world.GetEntity(entityId.Value);
                if (entity != null)
                {
                    entity.Attach(new RoomScoped());
                    _roomScopedEntities.Add(entityId.Value);
                }
            }
        }
    }

    private void PlacePlayerAt(Vector2 center)
    {
        if (_playerEntityId < 0) return;
        var entity = _world.GetEntity(_playerEntityId);
        if (entity == null) return;

        var body = entity.Get<Body>();
        var topLeft = center - body.HalfExtents;

        entity.Get<Position>().Value = topLeft;
        entity.Get<Velocity>().Value = Vector2.Zero;
        entity.Get<Grounded>().Value = false;

        var size = body.HalfExtents * 2f;
        entity.Get<CollisionActor>().Shape =
            new CollisionShape2D(BoundingBox2D.CreateFromPositionAndSize(topLeft, size));
    }

    internal void ApplyCheckpoint(Vector2 checkpointPos)
    {
        _currentRoom.Visit.RespawnPosition = checkpointPos;
    }
}
