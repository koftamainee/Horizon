using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoGame.Extended;

namespace Horizon.World;

public class Room
{
    public List<BoundingBox2D> Colliders { get; } = new();
    public BoundingBox2D Bounds { get; set; }

    public Room()
    {
        Bounds = BoundingBox2D.CreateFromPositionAndSize(new Vector2(0, 0), new Vector2(1600, 900));

        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(0, 700), new Vector2(1600, 32)));
        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(100, 550), new Vector2(200, 32)));
        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(500, 450), new Vector2(200, 32)));
        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(100, 100), new Vector2(200, 32)));
        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(0, 0), new Vector2(32, 900)));
        Colliders.Add(BoundingBox2D.CreateFromPositionAndSize(new Vector2(1568, 0), new Vector2(32, 900)));
    }
}
