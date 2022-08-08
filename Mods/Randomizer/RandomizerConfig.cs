using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using SMLHelper.V2.Json;
using SMLHelper.V2.Options;
using SMLHelper.V2.Options.Attributes;
using UnityEngine;
using UnityModdingUtility.Extensions;

namespace GRandomizer
{
    [Menu("Randomizer")]
    public class RandomizerConfig : ConfigFile
    {
        [Toggle("Randomize Lifepod Spawn Location")]
        public bool RandomSpawnLocation = true;

        [Slider("Random Spawn Location Intensity", 0f, 1f, Step = 0.01f, Format = "{0}")]
        public float RandomSpawnIntensity = 0.6f;

        [IgnoreMember]
        public float MaxSpawnRadius => Mathf.Pow(RandomSpawnIntensity, 1.5f) * RandomStart.kWorldExtents;

        [Toggle("Randomize Item Craft Time")]
        public bool RandomCraftDuration = true;

        [Toggle("Randomize Text"), OnChange(nameof(RandomLocalization_OnChange))]
        public bool RandomLocalization = false;

        void RandomLocalization_OnChange(ToggleChangedEventArgs e)
        {
            if (Language.main.Exists())
            {
                // Refresh all the localized text
                LocalizationRandomizer.SetCurrentLanguage_Patch.InvokeOnLanguageChanged();
            }
        }

        [Toggle("Randomize Loot")]
        public bool RandomLoot = true;

        [Toggle("Randomize Item Inventory Size")]
        public bool RandomItemSize = true;

        [Toggle("Randomize Colors")]
        public bool RandomColors = true;

        [Toggle("Randomize Creatures")]
        public bool RandomCreatures = false;

        [Choice("Randomize Dialogue", new string[] { "Off", "Same Speaker", "Random" })]
        public RandomDialogueMode RandomDialogue = RandomDialogueMode.Random;

        [Toggle("Ping Randomizer")]
        public bool PingRandomizer = true;

        [Toggle("Lifepod Randomizer")]
        public bool LifepodRandomizer = true;

        [Toggle("Animation Randomizer")]
        public bool AnimationRandomizer = true;

        //[Choice("Icon Randomizer", new string[] { "Off", "Same Group", "Random" })]
        //public SpriteRandomizer.Mode SpriteRandomizerMode = SpriteRandomizer.Mode.Random;
    }
}
