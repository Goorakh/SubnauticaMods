using System;

namespace GRandomizer.Util.Lifepod
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class LifepodModelTypeAttribute : Attribute
    {
        public readonly LifepodModelType Type;

        public LifepodModelTypeAttribute(LifepodModelType type)
        {
            Type = type;
        }
    }
}
