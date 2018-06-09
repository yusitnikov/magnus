using System;

namespace Magnus
{
    [Flags]
    enum Event
    {
        NONE = 0,
        TABLE_HIT = 1,
        FLOOR_HIT = 2,
        LOW_CROSS = 4,
        LOW_HIT = TABLE_HIT | FLOOR_HIT | LOW_CROSS,
        NET_CROSS = 8,
        MAX_HEIGHT = 16,
        BAT_HIT = 32,
        RIGHT_BAT_HIT = 64,
        LEFT_BAT_HIT = 128,
        ANY_HIT = TABLE_HIT | FLOOR_HIT | BAT_HIT
    }
}
