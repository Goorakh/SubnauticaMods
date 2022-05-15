using System;

namespace GRandomizer.Util
{
    [Flags]
    public enum TypeFlags : byte
    {
        None = 0,
        AllAssemblies = 1 << 0,
        AllExceptThisAssembly = 1 << 1,
        Interface = 1 << 2,
        Class = 1 << 3,
        ValueType = 1 << 4
    }
}
