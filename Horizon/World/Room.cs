using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace Horizon.World;

public class Room
{
    public List<Rectangle> Colliders { get; } = new();
    public Rectangle Bounds { get; set; }

    public Room()
    {
        Bounds = new Rectangle(0, 0, 1600, 900);
        
        Colliders.Add(new Rectangle(0, 700, 1600, 32));
        Colliders.Add(new Rectangle(100, 550, 200, 32));
        Colliders.Add(new Rectangle(500, 450, 200, 32));
        Colliders.Add(new Rectangle(100, 100, 200, 32));
        Colliders.Add(new Rectangle(0, 0, 32, 900));
        Colliders.Add(new Rectangle(1568, 0, 32, 900));
    }
}