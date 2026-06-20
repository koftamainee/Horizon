using MonoGame.Extended;
using MonoGame.Extended.Collisions;

namespace Horizon.ECS.Components;

public class CollisionActor : ICollisionActor
{
    public int Id { get; set; }
    public CollisionShape2D Shape { get; set; }
    public bool IsOneWay { get; set; }
    public bool IsHazard { get; set; }
}
