using System;

namespace GRandomizer.Util
{
    [Flags]
    public enum TypeFlags : byte
    {
        None = 0,
        OtherAssemblies = 1 << 0,
        ThisAssembly = 1 << 1,
        AllAssemblies = OtherAssemblies | ThisAssembly,
        Interface = 1 << 2,
        Class = 1 << 3,
        ValueType = 1 << 4
    }
}
