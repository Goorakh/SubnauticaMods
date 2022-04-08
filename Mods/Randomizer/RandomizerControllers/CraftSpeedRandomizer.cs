using GRandomizer.Util;
using HarmonyLib;

namespace GRandomizer.RandomizerControllers
{
    class CraftSpeedRandomizer : AutoSingleton<CraftSpeedRandomizer>, IRandomizerController
    {
        public bool IsEnabled()
        {
            return Mod.Config.RandomCraftDuration;
        }

        [HarmonyPatch(typeof(Crafter), nameof(Crafter.Craft))]
        static class Crafter_Craft_Patch
        {
            static void Prefix(ref float duration)
            {
                if (Instance.IsEnabled())
                {
                    duration *= UnityEngine.Random.Range(0.1f, 3f);
                }
            }
        }
    }
}
