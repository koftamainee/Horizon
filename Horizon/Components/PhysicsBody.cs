using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Horizon.Components;

public class PhysicsBody
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsGrounded { get; private set; }

    private readonly Vector2 _size;
    
    private const float Gravity = 1500f;
    private const float FallGravityMultiplier = 1.5f;

    public PhysicsBody(Vector2 position, Vector2 size)
    {
        Position = position;
        _size = size;
    }

    public void Update(float dt, List<Rectangle> colliders)
    {
        var velocity = Velocity;
        
        float gravityThisFrame = velocity.Y > 0
            ? Gravity * FallGravityMultiplier
            : Gravity;
        
        velocity.Y += gravityThisFrame * dt;
        Velocity = velocity;

        Position += new Vector2(Velocity.X * dt, 0);
        ResolveHorizontalCollisions(colliders);

        IsGrounded = false;
        Position += new Vector2(0, Velocity.Y * dt);
        ResolveVerticalCollisions(colliders);
    }

    private void ResolveHorizontalCollisions(List<Rectangle> colliders)
    {
        var bounds = GetBounds();
        foreach (var collider in colliders)
        {
            if (!bounds.Intersects(collider)) continue;

            if (Velocity.X < 0)
            {
                Position = new Vector2(collider.Right, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
            }
            else if (Velocity.X > 0)
            {
                Position = new Vector2(collider.Left - _size.X, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
            }
            bounds = GetBounds();
        }
    }

    private void ResolveVerticalCollisions(List<Rectangle> colliders)
    {
        var bounds = GetBounds();
        foreach (var collider in colliders)
        {
            if (!bounds.Intersects(collider)) continue;

            if (Velocity.Y < 0)
            {
                Position = new Vector2(Position.X, collider.Bottom);
                Velocity = new Vector2(Velocity.X, 0);
            }
            else if (Velocity.Y > 0)
            {
                Position = new Vector2(Position.X, collider.Top - _size.Y);
                Velocity = new Vector2(Velocity.X, 0);
                IsGrounded = true;
            }
            bounds = GetBounds();
        }
    }

    public Rectangle GetBounds() =>
        new((int)Position.X, (int)Position.Y, (int)_size.X, (int)_size.Y);
}