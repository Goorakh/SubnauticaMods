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
using UnityModdingUtility;
using UnityModdingUtility.Extensions;

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
            _craftDurations.Clear();
        }

        public static void Serialize(BinaryWriter writer)
        {
            writer.Write(_craftDurations);
        }

        public static void Deserialize(VersionedBinaryReader reader)
        {
            _craftDurations.SetTo(reader.ReadDictionary<TechType, float>());
        }

        struct RandomCraftDurationData
        {
            public readonly float Exponent; // Controls the bias of the value, higher values means shorter craft durations are more likely
            public readonly float MinValue; // The minimum possible craft duration
            public readonly float MaxValue; // The maximum possible craft duration

            public readonly float MinimumValueModifier;

            public RandomCraftDurationData(float exponent, float minValue, float maxValue) : this()
            {
                Exponent = exponent;
                MinValue = minValue;
                MaxValue = maxValue;

                MinimumValueModifier = Mathf.Pow(minValue / maxValue, 1f / exponent);
            }

            public float GetRandomValue()
            {
                return (float)Math.Round(Mathf.Pow(((1f - MinimumValueModifier) * UnityEngine.Random.value) + MinimumValueModifier, Exponent) * MaxValue, 1);
            }
        }

        static readonly RandomCraftDurationData _itemDuration = new RandomCraftDurationData(6f, 0.75f, 30f);
        static readonly RandomCraftDurationData _vehicleDuration = new RandomCraftDurationData(2f, 5f, 30f);
        static readonly RandomCraftDurationData _basePieceDuration = new RandomCraftDurationData(2f, 0.01f, 10f);

        static readonly InitializeOnAccessDictionaryArg<TechType, float, RandomCraftDurationData> _craftDurations = new InitializeOnAccessDictionaryArg<TechType, float, RandomCraftDurationData>(durationData => durationData.GetRandomValue());

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
                        result = _craftDurations[techType, _itemDuration];
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
                        return _craftDurations[techType, _vehicleDuration];
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
                        return _craftDurations[__instance.GetCurrentStageTech(), _vehicleDuration];
                    }

                    return original;
                }
            }
        }

        [HarmonyPatch]
        static class Constructable_GetConstructionInterval_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(Constructable), nameof(Constructable.Construct));
                yield return AccessTools.DeclaredMethod(typeof(Constructable), nameof(Constructable.Deconstruct));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo Constructable_GetConstructInterval_MI = SymbolExtensions.GetMethodInfo(() => Constructable.GetConstructInterval());

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(Constructable_GetConstructInterval_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.GetConstructInterval_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetConstructInterval_MI = SymbolExtensions.GetMethodInfo(() => GetConstructInterval(default, default));
                static float GetConstructInterval(float constructInterval, Constructable __instance)
                {
                    if (IsEnabled())
                    {
                        return constructInterval * _craftDurations[__instance.techType, _basePieceDuration];
                    }

                    return constructInterval;
                }
            }
        }
    }
}
