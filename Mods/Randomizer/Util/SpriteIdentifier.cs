using GRandomizer.Util.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public struct SpriteIdentifier : IEquatable<SpriteIdentifier>
    {
        public readonly SpriteManager.Group Group;
        public readonly string Name;

        public SpriteIdentifier(VersionedBinaryReader reader)
        {
            Group = reader.ReadGeneric<SpriteManager.Group>();
            Name = reader.ReadString();
        }

        public SpriteIdentifier(SpriteManager.Group group, string name)
        {
            Group = group;
            Name = name;
        }

        public override bool Equals(object obj)
        {
            return obj is SpriteIdentifier identifier && Equals(identifier);
        }

        public bool Equals(SpriteIdentifier other)
        {
            return Group == other.Group &&
                   Name == other.Name;
        }

        public override int GetHashCode()
        {
            int hashCode = -570022382;
            hashCode = (hashCode * -1521134295) + Group.GetHashCode();
            hashCode = (hashCode * -1521134295) + EqualityComparer<string>.Default.GetHashCode(Name);
            return hashCode;
        }

        public class EqualityComparer : IEqualityComparer<SpriteIdentifier>
        {
            static EqualityComparer _instance;
            public static EqualityComparer Instance => _instance ?? (_instance = new EqualityComparer());

            public bool Equals(SpriteIdentifier x, SpriteIdentifier y)
            {
                return x.Equals(y);
            }

            public int GetHashCode(SpriteIdentifier obj)
            {
                return obj.GetHashCode();
            }
        }
    }
}
