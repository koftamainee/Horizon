using System;
using Horizon.Input;
using Horizon.Physics;
using Microsoft.Xna.Framework;

namespace Horizon.Components;

public class PlayerController
{
    private readonly PhysicsBody _body;
    private readonly InputSystem _input;

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

    public PlayerController(PhysicsBody body, InputSystem input)
    {
        _body = body;
        _input = input;
        _jumpsRemaining = MaxJumps;
    }

    public void Update(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        _coyoteTimer -= dt;
        _jumpBufferTimer -= dt;
        _dashCooldownTimer -= dt;

        if (_body.IsGrounded)
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
            var velocity = _body.Velocity;
            velocity.X = _dashDirection * DashSpeed;
            velocity.Y = 0f;
            _body.Velocity = velocity;

            if (_dashTimer <= 0)
                _isDashing = false;

            return;
        }

        var vel = _body.Velocity;

        var inputX = 0f;
        if (_input.IsActionPressed(InputSystem.Actions.MoveLeft)) inputX -= 1;
        if (_input.IsActionPressed(InputSystem.Actions.MoveRight)) inputX += 1;

        if (inputX != 0)
            _dashDirection = inputX;

        float accel = _body.IsGrounded ? Acceleration : AirAcceleration;
        float decel = _body.IsGrounded ? Deceleration : AirDeceleration;

        vel.X = inputX != 0
            ? MoveTowards(vel.X, inputX * MoveSpeed, accel * dt)
            : MoveTowards(vel.X, 0, decel * dt);

        if (_jumpBufferTimer > 0 && _jumpsRemaining > 0)
        {
            bool firstJump = _coyoteTimer > 0 || _jumpsRemaining == MaxJumps;
            vel.Y = firstJump ? JumpSpeed : DoubleJumpSpeed;
            _jumpBufferTimer = 0;
            _isJumping = firstJump;
            _jumpsRemaining--;

            if (_coyoteTimer > 0)
                _coyoteTimer = 0;
        }

        if (_isJumping && _input.IsActionJustReleased(InputSystem.Actions.Jump) && vel.Y < 0)
        {
            vel.Y *= JumpCutMultiplier;
            _isJumping = false;
        }

        if (_input.IsActionJustPressed(InputSystem.Actions.Dash) && _dashCooldownTimer <= 0)
        {
            _isDashing = true;
            _dashTimer = DashDuration;
            _dashCooldownTimer = DashCooldown;
            _dashDirection = inputX != 0 ? inputX : (_dashDirection != 0 ? _dashDirection : 1f);
            vel.Y = 0f;
        }

        _body.Velocity = vel;
    }

    private static float MoveTowards(float current, float target, float maxDelta)
    {
        if (MathF.Abs(target - current) <= maxDelta)
            return target;
        return current + MathF.Sign(target - current) * maxDelta;
    }
}