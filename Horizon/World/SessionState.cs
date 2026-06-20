using System.Collections.Generic;

namespace Horizon.World;

public class SessionState
{
    public Dictionary<string, RoomState> Rooms { get; } = new();
}
