using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class CraftSpeedRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomCraftDuration;
        }

        const float CRAFT_TIME_EXP = 6f; // Controls the bias of the value, higher values means shorter craft durations are more likely
        const float CRAFT_TIME_MIN_VALUE = 0.75f; // The minimum possible craft duration
        const float CRAFT_TIME_MAX_VALUE = 60f; // The maximum possible craft duration
        static readonly float _minimumValueModifier = Mathf.Pow(CRAFT_TIME_MIN_VALUE / CRAFT_TIME_MAX_VALUE, 1f / CRAFT_TIME_EXP);

        static readonly InitializeOnAccessDictionary<TechType, float> _craftTimes = new InitializeOnAccessDictionary<TechType, float>(key =>
        {
            return (float)Math.Round(Mathf.Pow(((1f - _minimumValueModifier) * UnityEngine.Random.value) + _minimumValueModifier, CRAFT_TIME_EXP) * CRAFT_TIME_MAX_VALUE, 1);
        });

        [HarmonyPatch]
        static class CraftData_GetCraftTime_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => CraftData.GetCraftTime(default, out Discard<float>.Value));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method)
            {
                int techTypeArgIndex = method.FindArgumentIndex(typeof(TechType));
                int resultArgIndex = method.FindArgumentIndex(typeof(float).MakeByRefType());

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg, techTypeArgIndex);
                        yield return new CodeInstruction(OpCodes.Ldarg, resultArgIndex);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.Postfix_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Postfix_MI = SymbolExtensions.GetMethodInfo(() => Postfix(default, default, ref Discard<float>.Value));
                static bool Postfix(bool __result, TechType techType, ref float result)
                {
                    if (IsEnabled())
                    {
                        result = _craftTimes[techType];
                        return true;
                    }

                    return __result;
                }
            }
        }
    }
}
