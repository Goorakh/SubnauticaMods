using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util.Serialization
{
    public class VersionedBinaryReader : BinaryReader
    {
        public SaveVersion Version { get; private set; }

        public VersionedBinaryReader(Stream input) : base(input)
        {
        }

        public VersionedBinaryReader(Stream input, Encoding encoding) : base(input, encoding)
        {
        }

        public VersionedBinaryReader(Stream input, Encoding encoding, bool leaveOpen) : base(input, encoding, leaveOpen)
        {
        }

        public void Initialize()
        {
            Version = this.ReadGeneric<SaveVersion>();
        }
    }
}
