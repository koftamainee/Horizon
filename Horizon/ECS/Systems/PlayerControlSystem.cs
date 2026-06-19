using System;
using System.Collections.Generic;
using Horizon.ECS.Components;
using Horizon.Input;
using Microsoft.Xna.Framework;
using MonoGame.Extended.ECS;
using MonoGame.Extended.ECS.Systems;

namespace Horizon.ECS.Systems;

public class PlayerControlSystem : UpdateSystem
{
    private readonly InputSystem _input;
    private MonoGame.Extended.ECS.World _world;
    private readonly List<int> _players = new();

    private const float MoveSpeed = 300f;
    private const float Acceleration = 2000f;
    private const float Deceleration = 3000f;
    private const float AirAcceleration = 1200f;
    private const float AirDeceleration = 800f;

    private const float JumpSpeed = -700f;
    private const float DoubleJumpSpeed = -600f;
    private const float CoyoteTime = 0.1f;
    private const float JumpBufferTime = 0.1f;
    private const float JumpCutMultiplier = 0.5f;
    private const int MaxJumps = 2;

    private const float DashSpeed = 800f;
    private const float DashDuration = 0.1f;
    private const float DashCooldown = 0.6f;

    private float _coyoteTimer;
    private float _jumpBufferTimer;
    private bool _isJumping;
    private int _jumpsRemaining;
    private bool _isDashing;
    private float _dashTimer;
    private float _dashCooldownTimer;
    private float _dashDirection;

    public PlayerControlSystem(InputSystem input)
    {
        _input = input;
        _jumpsRemaining = MaxJumps;
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
        if (entity != null && entity.Has<PlayerTag>())
            _players.Add(entityId);
    }

    private void OnEntityRemoved(int entityId)
    {
        _players.Remove(entityId);
    }

    public override void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _coyoteTimer -= dt;
        _jumpBufferTimer -= dt;
        _dashCooldownTimer -= dt;

        foreach (int id in _players)
        {
            var entity = _world.GetEntity(id);
            if (entity == null) continue;

            var vel = entity.Get<Velocity>();
            var grounded = entity.Get<Grounded>();

            if (grounded.Value)
            {
                _coyoteTimer = CoyoteTime;
                _isJumping = false;
                _jumpsRemaining = MaxJumps;
            }
            else if (_coyoteTimer <= 0 && _jumpsRemaining >= MaxJumps)
            {
                _jumpsRemaining = MaxJumps - 1;
            }

            if (_input.IsActionJustPressed(InputSystem.Actions.Jump))
                _jumpBufferTimer = JumpBufferTime;

            if (_isDashing)
            {
                _dashTimer -= dt;
                vel.Value.X = _dashDirection * DashSpeed;
                vel.Value.Y = 0f;

                if (_dashTimer <= 0)
                    _isDashing = false;

                continue;
            }

            var inputX = 0f;
            if (_input.IsActionPressed(InputSystem.Actions.MoveLeft)) inputX -= 1;
            if (_input.IsActionPressed(InputSystem.Actions.MoveRight)) inputX += 1;

            if (inputX != 0)
                _dashDirection = inputX;

            float accel = grounded.Value ? Acceleration : AirAcceleration;
            float decel = grounded.Value ? Deceleration : AirDeceleration;

            vel.Value.X = inputX != 0
                ? MoveTowards(vel.Value.X, inputX * MoveSpeed, accel * dt)
                : MoveTowards(vel.Value.X, 0, decel * dt);

            if (_jumpBufferTimer > 0 && _jumpsRemaining > 0)
            {
                bool firstJump = _coyoteTimer > 0 || _jumpsRemaining == MaxJumps;
                vel.Value.Y = firstJump ? JumpSpeed : DoubleJumpSpeed;
                _jumpBufferTimer = 0;
                _isJumping = firstJump;
                _jumpsRemaining--;

                if (_coyoteTimer > 0)
                    _coyoteTimer = 0;
            }

            if (_isJumping && _input.IsActionJustReleased(InputSystem.Actions.Jump) && vel.Value.Y < 0)
            {
                vel.Value.Y *= JumpCutMultiplier;
                _isJumping = false;
            }

            if (_input.IsActionJustPressed(InputSystem.Actions.Dash) && _dashCooldownTimer <= 0)
            {
                _isDashing = true;
                _dashTimer = DashDuration;
                _dashCooldownTimer = DashCooldown;
                _dashDirection = inputX != 0 ? inputX : (_dashDirection != 0 ? _dashDirection : 1f);
                vel.Value.Y = 0f;
            }
        }
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;
        return current + MathF.Sign(target - current) * maxDelta;
    }
}
