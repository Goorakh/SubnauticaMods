using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Reflection;
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

        [HarmonyPatch]
        static class CraftData_GetCraftTime_Patch
        {
            static MethodBase TargetMethod()
            {
                float _float;
                return SymbolExtensions.GetMethodInfo(() => CraftData.GetCraftTime(default, out _float));
            }

            static void Postfix(bool __result, TechType techType, ref float result)
            {
                if (__result && IsEnabled())
                {
                    result = _craftTimes[techType];
                }
            }
        }
    }
}
