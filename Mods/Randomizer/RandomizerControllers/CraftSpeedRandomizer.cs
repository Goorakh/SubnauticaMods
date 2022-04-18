using GRandomizer.Util;
using HarmonyLib;
using System;
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

        static readonly InitializeOnAccessDictionary<TechType, float> _craftTimes = new InitializeOnAccessDictionary<TechType, float>(key =>
        {
            return (float)Math.Round(Mathf.Pow(UnityEngine.Random.value, 6f) * 60f, 1);
        });

        [HarmonyPatch(typeof(Crafter), nameof(Crafter.Craft))]
        static class Crafter_Craft_Patch
        {
            static void Prefix(TechType techType, ref float duration)
            {
                if (IsEnabled())
                {
                    duration = _craftTimes[techType];
                }
            }
        }
    }
}
