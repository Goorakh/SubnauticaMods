using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class CraftSpeedRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomCraftDuration;
        }

        static void Reset()
        {
            _craftTimes.Clear();
        }

        public static void Serialize(BinaryWriter writer)
        {
            writer.Write(_craftTimes);
        }

        public static void Deserialize(BinaryReader reader, ushort version)
        {
            _craftTimes.SetTo(reader.ReadDictionary<TechType, float>());
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

        [HarmonyPatch]
        static class ConstructorInput_Craft_Patch
        {
            static MethodInfo TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(ConstructorInput), nameof(ConstructorInput.Craft));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                int durationParameterIndex = original.FindArgumentIndex(typeof(float), "duration");
                int techTypeParameterIndex = original.FindArgumentIndex(typeof(TechType), "techType");

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.IsStarg(durationParameterIndex))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg, techTypeParameterIndex);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.GetDuration_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetDuration_MI = SymbolExtensions.GetMethodInfo(() => GetDuration(default, default));
                static float GetDuration(float original, TechType techType)
                {
                    if (IsEnabled())
                    {
                        return _craftTimes[techType];
                    }

                    return original;
                }
            }
        }

        [HarmonyPatch]
        static class Rocket_StartRocketConstruction_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<Rocket>(_ => _.StartRocketConstruction());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo VFXConstructing_timeToConstruct_FI = AccessTools.DeclaredField(typeof(VFXConstructing), nameof(VFXConstructing.timeToConstruct));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.StoresField(VFXConstructing_timeToConstruct_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.GetDuration_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static MethodInfo GetDuration_MI = SymbolExtensions.GetMethodInfo(() => GetDuration(default, default));
                static float GetDuration(float original, Rocket __instance)
                {
                    if (IsEnabled() && __instance.Exists())
                    {
                        return _craftTimes[__instance.GetCurrentStageTech()];
                    }

                    return original;
                }
            }
        }
    }
}
