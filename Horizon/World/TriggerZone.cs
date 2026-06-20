using MonoGame.Extended;

namespace Horizon.World;

public class TriggerZone
{
    public string Type { get; set; }
    public string TargetRoom { get; set; }
    public string TargetSpawn { get; set; }
    public BoundingBox2D Bounds { get; set; }
}
