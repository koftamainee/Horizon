using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Horizon.Physics;

public class PhysicsBody
{
    public Vector2 Position { get; set; }
    public Vector2 Velocity { get; set; }
    public bool IsGrounded { get; private set; }

    public IShape Shape { get; }

    private const float Gravity = 1500f;
    private const float FallGravityMultiplier = 1.5f;

    public PhysicsBody(Vector2 position, IShape shape)
    {
        Position = position;
        Shape = shape;
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
        var localBounds = Shape.Bounds;
        var bounds = GetBounds();

        foreach (var collider in colliders)
        {
            if (!bounds.Intersects(collider)) continue;

            if (Velocity.X < 0)
            {
                Position = new Vector2(collider.Right - localBounds.X, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
            }
            else if (Velocity.X > 0)
            {
                Position = new Vector2(collider.Left - localBounds.X - localBounds.Width, Position.Y);
                Velocity = new Vector2(0, Velocity.Y);
            }
            bounds = GetBounds();
        }
    }

    private void ResolveVerticalCollisions(List<Rectangle> colliders)
    {
        var localBounds = Shape.Bounds;
        var bounds = GetBounds();

        foreach (var collider in colliders)
        {
            if (!bounds.Intersects(collider)) continue;

            if (Velocity.Y < 0)
            {
                Position = new Vector2(Position.X, collider.Bottom - localBounds.Y);
                Velocity = new Vector2(Velocity.X, 0);
            }
            else if (Velocity.Y > 0)
            {
                Position = new Vector2(Position.X, collider.Top - localBounds.Y - localBounds.Height);
                Velocity = new Vector2(Velocity.X, 0);
                IsGrounded = true;
            }
            bounds = GetBounds();
        }
    }

    public Rectangle GetBounds()
    {
        var localBounds = Shape.Bounds;
        return new Rectangle(
            (int)Position.X + localBounds.X,
            (int)Position.Y + localBounds.Y,
            localBounds.Width,
            localBounds.Height);
    }
}