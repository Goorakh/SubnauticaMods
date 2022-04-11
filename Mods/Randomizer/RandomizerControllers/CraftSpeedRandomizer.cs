using GRandomizer.Util;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class CraftSpeedRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomCraftDuration;
        }

        static readonly Dictionary<TechType, float> _craftTimes = new Dictionary<TechType, float>();
        static float getCraftTime(TechType type)
        {
            if (_craftTimes.TryGetValue(type, out float craftTime))
            {
                return craftTime;
            }
            else
            {
                return _craftTimes[type] = Mathf.Pow(UnityEngine.Random.value, 5f) * 60f;
            }
        }

        [HarmonyPatch(typeof(Crafter), nameof(Crafter.Craft))]
        static class Crafter_Craft_Patch
        {
            static void Prefix(TechType techType, ref float duration)
            {
                if (IsEnabled())
                {
                    duration = getCraftTime(techType);
                }
            }
        }
    }
}
