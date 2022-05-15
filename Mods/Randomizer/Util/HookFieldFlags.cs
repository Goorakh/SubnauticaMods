using System;

namespace GRandomizer.Util
{
    [Flags]
    public enum HookFieldFlags : byte
    {
        None = 0,
        Ldfld = 1 << 0,
        Stfld = 1 << 1,
        IncludeInstance = 1 << 2
    }

}
