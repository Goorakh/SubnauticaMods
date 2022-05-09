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

        static readonly InitializeOnAccessDictionary<TechType, float> _craftTimes = new InitializeOnAccessDictionary<TechType, float>(key =>
        {
            return (float)Math.Round((Mathf.Pow(UnityEngine.Random.value, 6f) + (1f / 60f)) * 60f, 1);
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
                        yield return new CodeInstruction(OpCodes.Dup); // Dup return value
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
}
