using GRandomizer.RandomizerControllers;
using System;
using System.Collections.Generic;
using System.IO;

namespace GRandomizer.Util.Serialization
{
    public struct SaveDataContainer
    {
        const ushort NEWEST_SAVE_DATA_VERSION = 1;

        public readonly ushort Version;

        public SaveDataContainer(BinaryReader reader)
        {
            if (reader != null)
            {
                Version = reader.ReadUInt16();

                CraftSpeedRandomizer.Deserialize(reader, Version);
                DialogueRandomizer.Deserialize(reader, Version);
                ItemSizeRandomizer.Deserialize(reader, Version);
                LifepodRandomizer.Deserialize(reader, Version);
                LootRandomizer.Deserialize(reader, Version);
                PingRandomizer.Deserialize(reader, Version);

                if (Version > 0)
                {
                    AnimationRandomizer.Deserialize(reader, Version);
                }

                if (Version > NEWEST_SAVE_DATA_VERSION)
                {
                    throw new NotImplementedException($"Save data version {Version} is not implemented");
                }
            }
            else
            {
                Version = NEWEST_SAVE_DATA_VERSION;
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(NEWEST_SAVE_DATA_VERSION);

            CraftSpeedRandomizer.Serialize(writer);
            DialogueRandomizer.Serialize(writer);
            ItemSizeRandomizer.Serialize(writer);
            LifepodRandomizer.Serialize(writer);
            LootRandomizer.Serialize(writer);
            PingRandomizer.Serialize(writer);
            AnimationRandomizer.Serialize(writer);
        }
    }
}
