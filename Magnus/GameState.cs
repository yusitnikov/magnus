using System;

namespace Magnus
{
    [Flags]
    enum GameState
    {
        Serving = 1,
        Served = 2,
        FlyingToTable = 4,
        FlyingToBat = 8,
        Playing = FlyingToTable | FlyingToBat,
        ReadyToHit = Serving | FlyingToBat,
        NotReadyToHit = Served | FlyingToTable,
        Failed = 16
    }
}
