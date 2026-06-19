using Microsoft.Xna.Framework;

namespace Horizon.Physics;

public interface IShape
{
    ShapeType Type { get; }
    Rectangle Bounds { get; }
}