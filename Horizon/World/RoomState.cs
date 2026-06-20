using System.Collections.Generic;

namespace Horizon.World;

public class RoomState
{
    public HashSet<string> KilledEnemies { get; } = new();
}
