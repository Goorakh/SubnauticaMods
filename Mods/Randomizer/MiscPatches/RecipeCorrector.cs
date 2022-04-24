using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GRandomizer.MiscPatches
{
    static class RecipeCorrector
    {
        static readonly InitializeOnAccess<HashSet<TechType>> _ingredientsToCorrect = new InitializeOnAccess<HashSet<TechType>>(() =>
        {
            return CraftData.harvestOutputList.Values
                            .AddItem(TechType.CreepvineSeedCluster) // TODO: Don't hardcode these
                            .AddItem(TechType.StalkerTooth)
                            .AddItem(TechType.Titanium)
                            .AddItem(TechType.Copper)
                            .AddItem(TechType.Lead)
                            .AddItem(TechType.Silver)
                            .AddItem(TechType.Gold)
                            .AddItem(TechType.Lithium)
                            .AddItem(TechType.Diamond)
                            //.AddItem(TechType.CrashPowder)
                            .ToHashSet();
        });

        static class ReplaceRecipes
        {
            [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.TryPickupSingle))]
            static class CrafterLogic_TryPickupSingle_Patch
            {
                static void Prefix(ref TechType techType)
                {
                    if (LootRandomizer.IsEnabled() && _ingredientsToCorrect.Get.Contains(techType))
                        LootRandomizer.TryReplaceItem(ref techType);
                }
            }

            [HarmonyPatch]
            static class TooltipReplacer
            {
                static IEnumerable<MethodInfo> TargetMethods()
                {
                    string _string;
                    yield return SymbolExtensions.GetMethodInfo(() => TooltipFactory.BuildTech(default, default, out _string, default));
                    yield return SymbolExtensions.GetMethodInfo(() => TooltipFactory.Recipe(default, default, out _string, default));
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
                {
                    int techTypeArgIndex = Array.FindIndex(method.GetParameters(), p => p.ParameterType == typeof(TechType));

                    LocalBuilder originalTechType = generator.DeclareLocal(typeof(TechType));
                    List<CodeInstruction> prefix = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg, techTypeArgIndex),
                        new CodeInstruction(OpCodes.Dup, techTypeArgIndex),

                        new CodeInstruction(OpCodes.Stloc, originalTechType),

                        new CodeInstruction(OpCodes.Call, Hooks.getReplacementTechType_MI),
                        new CodeInstruction(OpCodes.Starg, techTypeArgIndex)
                    };

                    MethodInfo CraftData_Get_MI = SymbolExtensions.GetMethodInfo(() => CraftData.Get(default, default));

                    List<CodeInstruction> instructionsList = instructions.ToList();
                    for (int i = 0; i < instructionsList.Count; i++)
                    {
                        if (instructionsList[i].Calls(CraftData_Get_MI))
                        {
                            for (int j = i - 1; j >= 0; j--)
                            {
                                CodeInstruction instruction = instructionsList[j];
                                if (instruction.IsLdarg(techTypeArgIndex) ||
                                    (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo mi && mi.ReturnType == typeof(TechType))) // In case another mod has replaced the instruction :)
                                {
                                    instruction.opcode = OpCodes.Ldloc;
                                    instruction.operand = originalTechType;
                                    break;
                                }
                            }
                        }
                    }

                    return prefix.Concat(instructionsList);
                }
            }

            [HarmonyPatch(typeof(uGUI_CraftNode), nameof(uGUI_CraftNode.CreateIcon))]
            static class uGUI_CraftNode_CreateIcon_Patch
            {
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    MethodInfo SpriteManager_Get_MI = SymbolExtensions.GetMethodInfo(() => SpriteManager.Get(default));

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (instruction.Calls(SpriteManager_Get_MI))
                        {
                            yield return new CodeInstruction(OpCodes.Call, Hooks.getReplacementTechType_MI);
                        }

                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo getReplacementTechType_MI = SymbolExtensions.GetMethodInfo(() => getReplacementTechType(default));
                static TechType getReplacementTechType(TechType original)
                {
                    if (LootRandomizer.IsEnabled() && _ingredientsToCorrect.Get.Contains(original))
                        return LootRandomizer.TryGetItemReplacement(original);

                    return original;
                }
            }
        }

        [HarmonyPatch]
        static class IIngredient_get_techType_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                return from assembly in AppDomain.CurrentDomain.GetAssemblies()
                       from type in assembly.GetTypes()
                       where type.ImplementInterface(typeof(IIngredient))
                       select AccessTools.PropertyGetter(type, nameof(IIngredient.techType));
            }

            static TechType Postfix(TechType __result)
            {
                if (ExcludeSML_Patch.IsFromSML || !LootRandomizer.IsEnabled() || !_ingredientsToCorrect.Get.Contains(__result))
                    return __result;

                return LootRandomizer.TryGetItemReplacement(__result);
            }
        }

        [HarmonyPatch]
        [HarmonyBefore(GRConstants.SML_HELPER_HARMONY_ID)]
        static class ExcludeSML_Patch
        {
            static readonly FieldInfo IsFromSML_FI = AccessTools.Field(typeof(ExcludeSML_Patch), nameof(IsFromSML));
            public static bool IsFromSML = false;

            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo(() => SMLHelper.V2.Handlers.CraftDataHandler.ConvertToTechData(default));
                yield return SymbolExtensions.GetMethodInfo(() => SMLHelper.V2.Patchers.CraftDataPatcher.NeedsPatchingCheckPrefix(default));
                yield return SymbolExtensions.GetMethodInfo(() => SMLHelper.V2.Patchers.CraftDataPatcher.PatchCustomTechData());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo IIngredient_get_techType_MI = AccessTools.PropertyGetter(typeof(IIngredient), nameof(IIngredient.techType));

                foreach (CodeInstruction instruction in instructions)
                {
                    bool patchThisInstruction = instruction.Calls(IIngredient_get_techType_MI);

                    if (patchThisInstruction)
                    {
                        // IsFromSML = true;
                        yield return new CodeInstruction(OpCodes.Ldc_I4_1);
                        yield return new CodeInstruction(OpCodes.Stsfld, IsFromSML_FI);
                    }

                    yield return instruction;

                    if (patchThisInstruction)
                    {
                        // IsFromSML = false;
                        yield return new CodeInstruction(OpCodes.Ldc_I4_0);
                        yield return new CodeInstruction(OpCodes.Stsfld, IsFromSML_FI);
                    }
                }
            }
        }
    }
}
