using System;

namespace GRandomizer.Util
{
    [Flags]
    enum ColliderFlags : byte
    {
        None = 0,
        NonTrigger = 1 << 0,
        Trigger = 1 << 1
    }
}
