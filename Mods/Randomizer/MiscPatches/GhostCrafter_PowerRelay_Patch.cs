using GRandomizer.Util;
using GRandomizer.Util.FabricatorPower;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using UnityModdingUtility.Extensions;

namespace GRandomizer.MiscPatches
{
    static class GhostCrafter_PowerRelayPatch
    {
        const float CRAFT_COST = 5f;

        [HarmonyPatch]
        static class GhostCrafter_HasEnoughPower_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<GhostCrafter>(_ => _.HasEnoughPower());
            }

            static bool Postfix(bool __result, GhostCrafter __instance)
            {
                if (GameModeUtils.RequiresPower() && __instance.powerRelay == null)
                {
                    FabricatorPowerSource powerSource = __instance.GetComponent<FabricatorPowerSource>();
                    if (powerSource.Exists())
                    {
                        __result |= powerSource.HasEnoughPower(CRAFT_COST);
                    }
                }

                return __result;
            }
        }

        [HarmonyPatch]
        static class GhostCrafter_Craft_Patch
        {
            static MethodInfo TargetMethod()
            {
                return AccessTools.DeclaredMethod(typeof(GhostCrafter), nameof(GhostCrafter.Craft));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo CrafterLogic_ConsumeEnergy_MI = SymbolExtensions.GetMethodInfo(() => CrafterLogic.ConsumeEnergy(default, default));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(CrafterLogic_ConsumeEnergy_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.consumeEnergy_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo consumeEnergy_MI = SymbolExtensions.GetMethodInfo(() => consumeEnergy(default, default));
                static bool consumeEnergy(bool __result, GhostCrafter __instance)
                {
                    if (GameModeUtils.RequiresPower() && __instance.powerRelay == null)
                    {
                        FabricatorPowerSource powerSource = __instance.GetComponent<FabricatorPowerSource>();
                        if (powerSource.Exists())
                        {
                            __result |= powerSource.ConsumeEnergy(CRAFT_COST);
                        }
                    }

                    return __result;
                }
            }
        }
    }
}
