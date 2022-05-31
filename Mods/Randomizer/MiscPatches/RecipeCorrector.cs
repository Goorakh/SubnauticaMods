using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GRandomizer.MiscPatches
{
    static class RecipeCorrector
    {
        static readonly InitializeOnAccess<HashSet<TechType>> _ingredientsToCorrect = new InitializeOnAccess<HashSet<TechType>>(() =>
        {
            return CraftData.harvestOutputList.Values
                            .Concat(new TechType[] // TODO: Don't hardcode these
                            {
                                TechType.CreepvineSeedCluster,
                                TechType.StalkerTooth,
                                TechType.Titanium,
                                TechType.Copper,
                                TechType.Lead,
                                TechType.Silver,
                                TechType.Gold,
                                TechType.Lithium,
                                TechType.Diamond,
                                //TechType.CrashPowder,
                                TechType.GasPod
                            }).ToHashSet();
        });

        static class ReplaceRecipes
        {
            [HarmonyPatch(typeof(CrafterLogic), nameof(CrafterLogic.TryPickupSingle))]
            static class CrafterLogic_TryPickupSingle_Patch
            {
                static void Prefix(ref TechType techType)
                {
                    techType = Hooks.GetReplacementTechType(techType);
                }
            }

            [HarmonyPatch]
            static class CrafterGhostModel_GetGhostModel_Patch
            {
                static MethodInfo TargetMethod()
                {
                    return SymbolExtensions.GetMethodInfo(() => CrafterGhostModel.GetGhostModel(default));
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                {
                    LocalGenerator localGen = new LocalGenerator(generator);

                    MethodInfo CraftData_GetPrefabForTechType_MI = SymbolExtensions.GetMethodInfo(() => CraftData.GetPrefabForTechType(default, default));

                    FieldInfo Pickupable_cubeOnPickup_FI = AccessTools.Field(typeof(Pickupable), nameof(Pickupable.cubeOnPickup));

                    bool isWaitingForPrefabLocal = false;
                    int prefabLocalIndex = -1;

                    LocalBuilder originalTechType = localGen.GetLocal(typeof(TechType), false);

                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Dup);
                    yield return new CodeInstruction(OpCodes.Stloc, originalTechType);
                    yield return new CodeInstruction(OpCodes.Call, CrafterHooks.tryGetItemReplacement_MI);
                    yield return new CodeInstruction(OpCodes.Starg, 0);

                    LocalBuilder originalPrefab = localGen.GetLocal(typeof(GameObject), false);

                    yield return new CodeInstruction(OpCodes.Ldnull);
                    yield return new CodeInstruction(OpCodes.Stloc, originalPrefab);

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (prefabLocalIndex == -1)
                        {
                            if (!isWaitingForPrefabLocal)
                            {
                                if (instruction.Calls(CraftData_GetPrefabForTechType_MI))
                                {
                                    ParameterInfo[] parameters = CraftData_GetPrefabForTechType_MI.GetParameters();
                                    LocalBuilder[] locals = new LocalBuilder[parameters.Length - 1];
                                    for (int i = parameters.Length - 1; i >= 1; i--)
                                    {
                                        locals[i - 1] = localGen.GetLocal(parameters[i].ParameterType, false);
                                        yield return new CodeInstruction(OpCodes.Stloc, locals[i - 1]);
                                    }

                                    yield return new CodeInstruction(OpCodes.Ldloc, originalTechType);
                                    for (int i = 1; i < parameters.Length; i++)
                                    {
                                        yield return new CodeInstruction(OpCodes.Ldloc, locals[i - 1]);
                                    }

                                    yield return instruction;
                                    yield return new CodeInstruction(OpCodes.Stloc, originalPrefab);

                                    for (int i = 1; i < parameters.Length; i++)
                                    {
                                        yield return new CodeInstruction(OpCodes.Ldloc, locals[i - 1]);
                                        localGen.ReleaseLocal(locals[i - 1]);
                                    }

                                    isWaitingForPrefabLocal = true;
                                }
                            }
                            else if (instruction.IsStloc())
                            {
                                prefabLocalIndex = instruction.GetLocalIndex();
                                isWaitingForPrefabLocal = false;
                            }
                        }

                        if (instruction.opcode == OpCodes.Ret)
                        {
                            if (prefabLocalIndex == -1)
                            {
                                Utils.LogError("Could not locate prefabLocalIndex!", true);
                            }
                            else
                            {
                                yield return new CodeInstruction(OpCodes.Ldloc, prefabLocalIndex);
                                yield return new CodeInstruction(OpCodes.Ldloc, originalPrefab);
                                yield return new CodeInstruction(OpCodes.Call, CrafterHooks.Postfix_MI);
                            }
                        }

                        yield return instruction;

                        if (instruction.LoadsField(Pickupable_cubeOnPickup_FI))
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, originalTechType);
                            yield return new CodeInstruction(OpCodes.Call, CrafterHooks.Pickupable_cubeOnPickup_Hook_MI);
                        }
                    }
                }

                static class CrafterHooks
                {
                    public static readonly MethodInfo Postfix_MI = SymbolExtensions.GetMethodInfo(() => Postfix(default, default, default));
                    static GameObject Postfix(GameObject __result, GameObject prefab, GameObject originalPrefab)
                    {
                        if (!LootRandomizer.IsEnabled() || __result.Exists() || prefab == originalPrefab)
                            return __result;

                        GameObject obj = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity, false);

#if VERBOSE
                        Utils.DebugLog($"obj [{obj.GetComponents<MonoBehaviour>().Join(m => m.GetType().FullName)}]");
                        Utils.DebugLog($"originalPrefab [{originalPrefab.GetComponents<MonoBehaviour>().Join(m => m.GetType().FullName)}]");
#endif

                        obj.EnsureComponent<Pickupable>();

                        obj.PrepareStaticItem();
                        obj.DisableAllCollidersOfType<Collider>();
                        obj.RemoveAllComponentsNotIn(originalPrefab);

                        // Stops the item from being reparented
                        LargeWorldEntity largeWorldEntity = obj.GetComponent<LargeWorldEntity>();
                        if (largeWorldEntity.Exists())
                            largeWorldEntity.enabled = false;

                        obj.SetActive(true);

                        obj.AddVFXFabricatingComponentIfMissing(false);
                        return obj;
                    }

                    public static readonly MethodInfo tryGetItemReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetItemReplacement(default));
                    static TechType tryGetItemReplacement(TechType original)
                    {
                        return _ingredientsToCorrect.Get.Contains(original) ? LootRandomizer.TryGetItemReplacement(original) : original;
                    }

                    public static readonly MethodInfo Pickupable_cubeOnPickup_Hook_MI = SymbolExtensions.GetMethodInfo(() => Pickupable_cubeOnPickup_Hook(default, default));
                    static bool Pickupable_cubeOnPickup_Hook(bool cubeOnPickup, TechType originalTechType)
                    {
                        return cubeOnPickup && !LootRandomizer.IsEnabled() && !_ingredientsToCorrect.Get.Contains(originalTechType);
                    }
                }
            }

            [HarmonyPatch]
            static class TooltipReplacer
            {
                static IEnumerable<MethodInfo> TargetMethods()
                {
                    yield return SymbolExtensions.GetMethodInfo(() => TooltipFactory.BuildTech(default, default, out Discard<string>.Value, default));
                    yield return SymbolExtensions.GetMethodInfo(() => TooltipFactory.Recipe(default, default, out Discard<string>.Value, default));
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase method, ILGenerator generator)
                {
                    int techTypeArgIndex = method.FindArgumentIndex(typeof(TechType));

                    LocalBuilder originalTechType = generator.DeclareLocal(typeof(TechType));
                    List<CodeInstruction> prefix = new List<CodeInstruction>
                    {
                        new CodeInstruction(OpCodes.Ldarg, techTypeArgIndex),
                        new CodeInstruction(OpCodes.Dup),

                        new CodeInstruction(OpCodes.Stloc, originalTechType),

                        new CodeInstruction(OpCodes.Call, Hooks.GetReplacementTechType_MI),
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
                                if (instruction.IsLdarg(techTypeArgIndex))
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
                            yield return new CodeInstruction(OpCodes.Call, Hooks.GetReplacementTechType_MI);
                        }

                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetReplacementTechType_MI = SymbolExtensions.GetMethodInfo(() => GetReplacementTechType(default));
                public static TechType GetReplacementTechType(TechType original)
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
                return typeof(IIngredient).GetImplementations(false, AccessTools.PropertyGetter(typeof(IIngredient), nameof(IIngredient.techType)));
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

        [HarmonyPatch]
        static class Inventory_Pickup_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<Inventory>(_ => _.Pickup(default, default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                LocalGenerator localGen = new LocalGenerator(generator);

                MethodInfo KnownTech_Analyze_MI = SymbolExtensions.GetMethodInfo(() => KnownTech.Analyze(default, default));
                ParameterInfo[] KnownTech_Analyze_Params = KnownTech_Analyze_MI.GetParameters();
                int techTypeIndex = KnownTech_Analyze_MI.FindArgumentIndex(typeof(TechType));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(KnownTech_Analyze_MI))
                    {
                        LocalBuilder[] locals = new LocalBuilder[KnownTech_Analyze_Params.Length - (techTypeIndex + 1)];
                        for (int i = locals.Length - 1; i >= 0; i--)
                        {
                            yield return new CodeInstruction(OpCodes.Stloc, locals[i] = localGen.GetLocal(KnownTech_Analyze_Params[i + (techTypeIndex + 1)].ParameterType, false));
                        }

                        yield return new CodeInstruction(OpCodes.Call, Hooks.Pickupable_GetTechType_Hook_MI);

                        for (int i = 0; i < locals.Length; i++)
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, locals[i]);
                            localGen.ReleaseLocal(locals[i]);
                        }
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Pickupable_GetTechType_Hook_MI = SymbolExtensions.GetMethodInfo(() => Pickupable_GetTechType_Hook(default));
                static TechType Pickupable_GetTechType_Hook(TechType techType)
                {
                    if (LootRandomizer.IsEnabled())
                    {
                        TechType originalType = LootRandomizer.TryGetOriginalItem(techType);
                        if (originalType != techType && _ingredientsToCorrect.Get.Contains(originalType))
                            return originalType;
                    }

                    return techType;
                }
            }
        }
    }
}
