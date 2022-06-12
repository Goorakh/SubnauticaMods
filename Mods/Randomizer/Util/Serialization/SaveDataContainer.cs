using GRandomizer.RandomizerControllers;
using System;
using System.Collections.Generic;
using System.IO;

namespace GRandomizer.Util.Serialization
{
    public struct SaveDataContainer
    {
        const SaveVersion NEWEST_SAVE_DATA_VERSION = SaveVersion.v0_0_2_0e;

        public SaveDataContainer(VersionedBinaryReader reader)
        {
            if (reader != null)
            {
                CraftSpeedRandomizer.Deserialize(reader);
                DialogueRandomizer.Deserialize(reader);
                ItemSizeRandomizer.Deserialize(reader);
                LifepodRandomizer.Deserialize(reader);
                LootRandomizer.Deserialize(reader);
                PingRandomizer.Deserialize(reader);

                if (reader.Version >= SaveVersion.v0_0_2_0b)
                {
                    AnimationRandomizer.Deserialize(reader);
                }

                if (reader.Version >= SaveVersion.v0_0_2_0e)
                {
                    SpriteRandomizer.Deserialize(reader);
                }

                if (reader.Version > NEWEST_SAVE_DATA_VERSION)
                {
                    throw new NotImplementedException($"Save data version {reader.Version} is not implemented");
                }
            }
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.WriteGeneric(NEWEST_SAVE_DATA_VERSION);

            CraftSpeedRandomizer.Serialize(writer);
            DialogueRandomizer.Serialize(writer);
            ItemSizeRandomizer.Serialize(writer);
            LifepodRandomizer.Serialize(writer);
            LootRandomizer.Serialize(writer);
            PingRandomizer.Serialize(writer);
            AnimationRandomizer.Serialize(writer);
            SpriteRandomizer.Serialize(writer);
        }
    }
}
