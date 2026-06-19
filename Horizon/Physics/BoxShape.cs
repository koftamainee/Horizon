using Microsoft.Xna.Framework;

namespace Horizon.Physics;

public sealed class BoxShape : IShape
{
    public Vector2 Offset { get; }
    public Vector2 HalfExtents { get; }

    public ShapeType Type => ShapeType.Box;

    public Rectangle Bounds
    {
        get
        {
            var topLeft = Offset - HalfExtents;
            var size = HalfExtents * 2f;
            return new Rectangle((int)topLeft.X, (int)topLeft.Y, (int)size.X, (int)size.Y);
        }
    }

    public BoxShape(Vector2 offset, Vector2 halfExtents)
    {
        Offset = offset;
        HalfExtents = halfExtents;
    }
}