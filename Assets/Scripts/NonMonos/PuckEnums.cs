using System;

namespace Pucks {
    [Flags]
    public enum EPuckMovementDirection {
        None            = 0b0,
        Stationary      = 0b1,
        Up              = 0b10,
        Down            = 0b100,
        Left            = 0b1000,
        Right           = 0b10000,
        SplitHorizontal = 0b100000,
        SplitVertical   = 0b1000000,
        Claimed         = 0b10000000
	}
}