using System;

namespace Magnus
{
    [Flags]
    enum Event
    {
        None = 0,
        TableHit = 1,
        FloorHit = 2,
        LowCross = 4,
        LowHit = TableHit | FloorHit | LowCross,
        NetCross = 8,
        MaxHeight = 16,
        BatHit = 32,
        RightBatHit = 64,
        LeftBatHit = 128,
        AnyHit = TableHit | FloorHit | BatHit
    }
}
