using GRandomizer.MiscPatches;
using GRandomizer.Util;
using HarmonyLib;
using QModManager.Utility;
using Story;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class LootRandomizer
    {
        static readonly InitializeOnAccess<TechType[]> _itemTypes = new InitializeOnAccess<TechType[]>(() =>
        {
            IEnumerable<TechType> getObtainableItems()
            {
                foreach (var techGroup in CraftData.groups)
                {
                    switch (techGroup.Key)
                    {
                        case TechGroup.Constructor: // Exclude vehicles in mobile vehicle bay
                        case TechGroup.BasePieces: // Exclude base pieces
                        case TechGroup.ExteriorModules: // Exclude base pieces
                        case TechGroup.InteriorPieces: // Exclude base pieces
                        case TechGroup.InteriorModules: // Exclude base pieces
                        case TechGroup.Miscellaneous: // Exclude base pieces
                            continue;
                    }

                    foreach (var category in techGroup.Value)
                    {
                        if (category.Key != TechCategory.Cyclops)  // Exclude cyclops blueprints
                        {
                            foreach (TechType item in category.Value)
                            {
                                yield return item;

                                if (CraftData.techData.TryGetValue(item, out CraftData.TechData techData))
                                {
                                    if (techData._linkedItems != null)
                                    {
                                        foreach (TechType linked in techData._linkedItems)
                                        {
                                            yield return linked;
                                        }
                                    }

                                    if (techData._ingredients != null)
                                    {
                                        foreach (CraftData.Ingredient ingredient in techData._ingredients)
                                        {
                                            if (ingredient != null)
                                                yield return ingredient._techType;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (string includeStr in ConfigReader.ReadFromFile<HashSet<string>>("Configs/ItemRandomizer::Include"))
                {
                    if (TechTypeExtensions.FromString(includeStr, out TechType includeType, true))
                    {
                        yield return includeType;
                    }
                    else
                    {
                        Utils.LogWarning($"Unknown TechType ({includeStr}) in ItemRandomizer.json Include list (are you missing a mod?)");
                    }
                }
            }

            IEnumerable<TechType> getBlacklistedItems()
            {
                foreach (string excludeStr in ConfigReader.ReadFromFile<HashSet<string>>("Configs/ItemRandomizer::Blacklist"))
                {
                    if (TechTypeExtensions.FromString(excludeStr, out TechType excludeType, true))
                    {
                        yield return excludeType;
                    }
                    else
                    {
                        Utils.LogWarning($"Unknown TechType ({excludeStr}) in ItemRandomizer.json Blacklist (are you missing a mod?)");
                    }
                }
            }

            HashSet<TechType> obtainableTypes = getObtainableItems().Except(getBlacklistedItems()).ToHashSet();

            int removed;
            if ((removed = obtainableTypes.RemoveWhere(type => CraftData.GetPrefabForTechType(type) == null)) > 0)
            {
                Utils.LogWarning($"Removing {removed} item types due to null prefab", true);
            }

            return obtainableTypes.ToArray();
        });

        static readonly InitializeOnAccess<DualDictionary<TechType, TechType>> _itemReplacementsDictionary = new InitializeOnAccess<DualDictionary<TechType, TechType>>(() =>
        {
            return new DualDictionary<TechType, TechType>(_itemTypes.Get.ToRandomizedReplacementDictionary());
        });

#if DEBUG
        static int _debugIndex = 0;
        static void logDebugIndex()
        {
#if VERBOSE
            Utils.DebugLog($"_debugIndex: {_debugIndex} ({_itemTypes.Get[_debugIndex]})", true);
#endif
        }
        public static void IncreaseDebugIndex()
        {
            if (++_debugIndex >= _itemTypes.Get.Length)
                _debugIndex = 0;

            logDebugIndex();
        }
        public static void DecreaseDebugIndex()
        {
            if (--_debugIndex < 0)
                _debugIndex = _itemTypes.Get.Length - 1;

            logDebugIndex();
        }
        public static void SetDebugIndex(int index)
        {
            _debugIndex = index;
            logDebugIndex();
        }
#endif

        const string NOT_IN_ITEM_DICT_LOG = "{0} (DE {1}) is not in the replacement dictionary, account for or exclude it!";

        public static readonly MethodInfo TryGetItemReplacement_MI = SymbolExtensions.GetMethodInfo(() => TryGetItemReplacement(default));
        public static TechType TryGetItemReplacement(TechType techType)
        {
            if (!IsEnabled() || techType == TechType.None)
                return techType;

#if DEBUG
            if (false && techType == TechType.Titanium)
            {
                return _itemTypes.Get[_debugIndex];
            }
#endif

            TechType discoveredEgg = EggPatch.ToDiscoveredEggType(techType);

            if (_itemReplacementsDictionary.Get.F2S_TryGetValue(discoveredEgg, out TechType replacementType))
                return EggPatch.CorrectEggType(replacementType);
            
            Utils.LogWarning(string.Format(NOT_IN_ITEM_DICT_LOG, techType, discoveredEgg));
            return techType;
        }

        public static TechType TryGetOriginalItem(TechType replaced)
        {
            if (!IsEnabled() || replaced == TechType.None)
                return replaced;

            TechType discoveredEgg = EggPatch.ToDiscoveredEggType(replaced);

            if (_itemReplacementsDictionary.Get.S2F_TryGetValue(discoveredEgg, out TechType originalType))
                return EggPatch.CorrectEggType(originalType);

            Utils.LogWarning(string.Format(NOT_IN_ITEM_DICT_LOG, replaced, discoveredEgg));
            return replaced;
        }

        public static readonly MethodInfo TryReplaceItem_MI = SymbolExtensions.GetMethodInfo(() => TryReplaceItem(ref Discard<TechType>.Value));
        public static void TryReplaceItem(ref TechType techType)
        {
            techType = TryGetItemReplacement(techType);
        }

        static readonly MethodInfo IsEnabled_MI = SymbolExtensions.GetMethodInfo(() => IsEnabled());
        public static bool IsEnabled()
        {
            return Mod.Config.RandomLoot;
        }

        [HarmonyPatch]
        static class LootSpawner_GetLoot_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<LootSpawner>(_ => _.GetEscapePodStorageTechTypes());
                yield return SymbolExtensions.GetMethodInfo<LootSpawner>(_ => _.GetSupplyTechTypes(default));
            }

            static TechType[] Postfix(TechType[] __result)
            {
                if (IsEnabled())
                {
                    TechType[] result = new TechType[Mathf.Max(__result.Length + UnityEngine.Random.Range(-2, 3), 1)];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = i < __result.Length ? TryGetItemReplacement(__result[i]) : _itemTypes.Get.GetRandom();
                    }

                    return result;
                }
                else
                {
                    return __result;
                }
            }
        }

        [HarmonyPatch(typeof(PickPrefab), nameof(PickPrefab.Start))]
        static class PickPrefab_Start_Patch
        {
            static void Prefix(PickPrefab __instance)
            {
                TryReplaceItem(ref __instance.pickTech);
            }
        }

        static IEnumerable<CodeInstruction> hookTechType(IEnumerable<CodeInstruction> instructions, int type)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.LoadsConstant(type))
                {
                    yield return new CodeInstruction(OpCodes.Call, TryGetItemReplacement_MI);
                }
            }
        }

        [HarmonyPatch]
        static class Replace_FireExtinguisher_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<FireExtinguisherHolder>(_ => _.TakeTank());
                yield return SymbolExtensions.GetMethodInfo<FireExtinguisherHolder>(_ => _.TryStoreTank());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return hookTechType(instructions, (int)TechType.FireExtinguisher);
            }
        }

        [HarmonyPatch]
        static class Replace_DepletedReactorRod_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<BaseNuclearReactor>(_ => _.SpawnDepletedRod());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return hookTechType(instructions, (int)TechType.DepletedReactorRod);
            }
        }

        [HarmonyPatch]
        static class Replace_BigFilteredWater_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.UpdateModel());
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterWater());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return hookTechType(instructions, (int)TechType.BigFilteredWater);
            }
        }

        [HarmonyPatch]
        static class Replace_Salt_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.UpdateModel());
                yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterSalt());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return hookTechType(instructions, (int)TechType.Salt);
            }
        }

        [HarmonyPatch(typeof(Drillable), nameof(Drillable.ChooseRandomResource))]
        static class Drillable_ChooseRandomResource_Patch
        {
            static readonly MethodInfo CraftData_GetPrefabForTechType_MI = SymbolExtensions.GetMethodInfo(() => CraftData.GetPrefabForTechType(default, default));
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                ParameterInfo[] methodArgs = CraftData_GetPrefabForTechType_MI.GetParameters();
                LocalBuilder[] methodArgLocals = new LocalBuilder[methodArgs.Length];
                for (int i = 1; i < methodArgLocals.Length; i++) // Don't declare local for first parameter since it won't be used
                {
                    methodArgLocals[i] = generator.DeclareLocal(methodArgs[i].ParameterType);
                }

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(CraftData_GetPrefabForTechType_MI))
                    {
                        for (int i = methodArgLocals.Length - 1; i > 0; i--) // Stloc for every parameter except the first one (TechType)
                        {
                            yield return new CodeInstruction(OpCodes.Stloc, methodArgLocals[i]);
                        }

                        yield return new CodeInstruction(OpCodes.Call, TryGetItemReplacement_MI); // Get replacement TechType

                        for (int i = 1; i < methodArgLocals.Length; i++) // Load all parameters back
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, methodArgLocals[i]);
                        }
                    }

                    yield return instruction;
                }
            }
        }

        [HarmonyPatch(typeof(Drillable), nameof(Drillable.GetDominantResourceType))]
        static class Drillable_GetDominantResourceType_Patch
        {
            static TechType Postfix(TechType __result)
            {
                return TryGetItemReplacement(__result);
            }
        }

        [HarmonyPatch(typeof(Drillable), nameof(Drillable.Start))]
        static class Drillable_Start_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo Drillable_renderers_FI = AccessTools.Field(typeof(Drillable), nameof(Drillable.renderers));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.StoresField(Drillable_renderers_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.replaceDrillableChunks_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo replaceDrillableChunks_MI = SymbolExtensions.GetMethodInfo(() => replaceDrillableChunks(default, default));
                static MeshRenderer[] replaceDrillableChunks(MeshRenderer[] renderers, Drillable __instance)
                {
                    if (!IsEnabled())
                        return renderers;

                    TechType techType = __instance.GetDominantResourceType();
                    if (techType == TechType.None)
                        return renderers;

                    for (int i = 0; i < renderers.Length; i++)
                    {
                        GameObject itemModel = CraftData.InstantiateFromPrefab(techType);
                        itemModel.PrepareStaticItem();
                        Pickupable pickupable = itemModel.GetComponent<Pickupable>();
                        if (pickupable.Exists())
                            pickupable.isPickupable = false;

                        itemModel.RemoveAllComponentsNotIn(renderers[i].gameObject);

                        itemModel.transform.SetParent(renderers[i].transform.parent);
                        itemModel.transform.localRotation = Utils.Random.Rotation;

                        // Don't blind the player with 10 light sources in the same spot
                        itemModel.DisableAllComponentsOfType<Light>();

                        itemModel.transform.localPosition = renderers[i].transform.localPosition;

                        if (itemModel.TryGetModelBounds(out Bounds modelBounds))
                        {
                            if (renderers[i].gameObject.TryGetModelBounds(out Bounds rendererBounds))
                            {
                                itemModel.transform.position += rendererBounds.center - modelBounds.center;

                                float scaleMult = rendererBounds.size.magnitude / modelBounds.size.magnitude;
                                if (scaleMult > 1.5f || scaleMult < (1 / 3f))
                                    itemModel.transform.localScale *= scaleMult;

                                if (!itemModel.HasComponentInChildren<Collider>())
                                {
                                    BoxCollider boxCollider = itemModel.AddComponent<BoxCollider>();
                                    boxCollider.size = modelBounds.size;
                                    boxCollider.center = modelBounds.center;
                                }
                            }
                            else
                            {
                                Utils.LogWarning($"[{__instance.name}] Could not get model bounds for renderers[{i}] ({renderers[i].name})", true);
                            }
                        }
                        else
                        {
                            Utils.LogWarning($"[{__instance.name}]: Could not get model bounds for {techType} ({itemModel.name})", true);
                        }

                        GameObject.Destroy(renderers[i].gameObject);
                        renderers[i] = itemModel.GetOrAddComponent<MeshRenderer>();
                    }

                    return renderers;
                }
            }
        }

        [HarmonyPatch(typeof(FiltrationMachine), nameof(FiltrationMachine.Spawn))]
        static class FiltrationMachine_Spawn_Patch
        {
            static bool Prefix(ref bool __result, ref Pickupable prefab, FiltrationMachine __instance)
            {
                if (IsEnabled())
                {
                    TechType newType = TryGetItemReplacement(prefab.GetTechType());

                    GameObject newPrefab = CraftData.GetPrefabForTechType(newType);
                    Pickupable pickupablePrefab = newPrefab.GetComponent<Pickupable>();
                    if (pickupablePrefab.Exists())
                    {
                        prefab = pickupablePrefab;
                        return true;
                    }

                    __result = SpawnNonPickupable(__instance, newPrefab);
                    return false;
                }

                return true;
            }

            [HarmonyPatch(typeof(FiltrationMachine), nameof(FiltrationMachine.Spawn))]
            [HarmonyReversePatch]
            static bool SpawnNonPickupable(FiltrationMachine __instance, GameObject prefab)
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    MethodInfo Pickupable_GetComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<Pickupable>(_ => _.GetComponent<Pickupable>());
                    MethodInfo GameObject_GetComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.GetComponent<Pickupable>());
                    MethodInfo GameObject_AddComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.AddComponent<Pickupable>());
                    MethodInfo Pickupable_GetTechType_MI = SymbolExtensions.GetMethodInfo<Pickupable>(_ => _.GetTechType());
                    MethodInfo CraftData_GetTechType_MI = SymbolExtensions.GetMethodInfo(() => CraftData.GetTechType(default));
                    MethodInfo Component_gameObject_get_MI = AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject));

                    foreach (CodeInstruction instruction in instructions.MethodReplacer(Pickupable_GetTechType_MI, CraftData_GetTechType_MI)
                                                                        .MethodReplacer(GameObject_GetComponent_Pickupable_MI, GameObject_AddComponent_Pickupable_MI))
                    {
                        if (instruction.Calls(Pickupable_GetComponent_Pickupable_MI) || instruction.Calls(Component_gameObject_get_MI))
                        {
                            // skip instruction
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }

                Transpiler(null);
                return default;
            }
        }

        static class FiltrationMachine_ItemModelReplacer
        {
            static readonly InitializeOnAccess<GameObject> _overrideSaltModel = new InitializeOnAccess<GameObject>(() => CraftData.GetPrefabForTechType(TryGetItemReplacement(TechType.Salt)));

            static readonly InitializeOnAccess<GameObject> _overrideWaterModel = new InitializeOnAccess<GameObject>(() => CraftData.GetPrefabForTechType(TryGetItemReplacement(TechType.BigFilteredWater)));

            static readonly FieldInfo FiltrationMachine_waterModel_FI = AccessTools.Field(typeof(FiltrationMachine), nameof(FiltrationMachine.waterModel));
            static readonly FieldInfo FiltrationMachine_saltModel_FI = AccessTools.Field(typeof(FiltrationMachine), nameof(FiltrationMachine.saltModel));

            [HarmonyPatch]
            static class FiltrationMachine_OverrideLoadModel_Patch
            {
                static IEnumerable<MethodInfo> TargetMethods()
                {
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.Start());
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.UpdateModel());
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    return instructions.HookField(FiltrationMachine_saltModel_FI, Hooks.Get_saltModel_Hook_MI, HookFieldFlags.Ldfld)
                                       .HookField(FiltrationMachine_waterModel_FI, Hooks.Get_waterModel_Hook_MI, HookFieldFlags.Ldfld);
                }

                static class Hooks
                {
                    public static readonly MethodInfo Get_saltModel_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_saltModel_Hook(default));
                    static GameObject Get_saltModel_Hook(GameObject saltModel)
                    {
                        return IsEnabled() ? _overrideSaltModel : saltModel;
                    }

                    public static readonly MethodInfo Get_waterModel_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_waterModel_Hook(default));
                    static GameObject Get_waterModel_Hook(GameObject waterModel)
                    {
                        return IsEnabled() ? _overrideWaterModel : waterModel;
                    }
                }
            }

            [HarmonyPatch(typeof(FiltrationMachine), nameof(FiltrationMachine.AssignModel))]
            static class FiltrationMachine_AssignModel_Patch
            {
                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    MethodInfo GameObject_GetComponent_VFXScan_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.GetComponent<VFXScan>());

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (instruction.Calls(GameObject_GetComponent_VFXScan_MI))
                        {
                            yield return new CodeInstruction(OpCodes.Dup); // Dup shownModel

                            yield return instruction;

                            yield return new CodeInstruction(OpCodes.Call, Hooks.GetOrAddComponent_MI);
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }

                static void Postfix(FiltrationMachine __instance)
                {
                    __instance.shownModel.PrepareStaticItem();
                    __instance.shownModel.RemoveAllComponentsNotIn(__instance.waterModel);
                    __instance.shownModel.AddVFXFabricatingComponentIfMissing(true);
                }

                static class Hooks
                {
                    public static readonly MethodInfo GetOrAddComponent_MI = SymbolExtensions.GetMethodInfo(() => GetComponent_Hook(default, default));
                    static VFXScan GetComponent_Hook(GameObject shownModel, VFXScan vfxComponent)
                    {
                        return vfxComponent.Exists() ? vfxComponent : shownModel.AddComponent<VFXScan>();
                    }
                }
            }
        }

        [HarmonyPatch]
        static class AddToInventory_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<BlueprintHandTarget>(_ => _.DisableGameObjectAsync()));
                yield return SymbolExtensions.GetMethodInfo<BlueprintHandTarget>(_ => _.UnlockBlueprint());
                yield return SymbolExtensions.GetMethodInfo<CoffeeVendingMachine>(_ => _.SpawnCoffee());
                yield return SymbolExtensions.GetMethodInfo<Creepvine>(_ => _.OnKnifeHit(default));
                yield return SymbolExtensions.GetMethodInfo<MedicalCabinet>(_ => _.OnHandClick(default));
                yield return SymbolExtensions.GetMethodInfo(() => PDAScanner.Scan());
                yield return SymbolExtensions.GetMethodInfo<Player>(_ => _.SetupCreativeMode());
                yield return SymbolExtensions.GetMethodInfo<UnlockItemData>(_ => _.Trigger());
                yield return SymbolExtensions.GetMethodInfo<VendingMachine>(_ => _.OnUse(default));
            }

            static readonly MethodInfo CraftData_AddToInventory_MI = SymbolExtensions.GetMethodInfo(() => CraftData.AddToInventory(default, default, default, default));
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                ParameterInfo[] methodArgs = CraftData_AddToInventory_MI.GetParameters();
                LocalBuilder[] methodArgLocals = new LocalBuilder[methodArgs.Length];
                for (int i = 1; i < methodArgLocals.Length; i++) // Don't declare local for first parameter since it won't be used
                {
                    methodArgLocals[i] = generator.DeclareLocal(methodArgs[i].ParameterType);
                }

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(CraftData_AddToInventory_MI))
                    {
                        for (int i = methodArgLocals.Length - 1; i > 0; i--) // Stloc for every parameter except the first one (TechType)
                        {
                            yield return new CodeInstruction(OpCodes.Stloc, methodArgLocals[i]);
                        }

                        yield return new CodeInstruction(OpCodes.Call, TryGetItemReplacement_MI); // Get replacement TechType

                        for (int i = 1; i < methodArgLocals.Length; i++) // Load all parameters back
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, methodArgLocals[i]);
                        }
                    }

                    yield return instruction;
                }
            }
        }

        [HarmonyPatch]
        static class CraftData_GetData_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo(() => CraftData.GetHarvestOutputData(default));
                yield return SymbolExtensions.GetMethodInfo(() => CraftData.GetCookedData(default));
            }

            static void Postfix(ref TechType __result)
            {
                TryReplaceItem(ref __result);
            }
        }

        [HarmonyPatch(typeof(Stalker), nameof(Stalker.LoseTooth))]
        static class Stalker_LoseTooth_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions.HookField(AccessTools.Field(typeof(Stalker), nameof(Stalker.toothPrefab)), Hooks.Get_toothPrefab_Hook_MI, HookFieldFlags.Ldfld);
            }

            static class Hooks
            {
                public static readonly MethodInfo Get_toothPrefab_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_toothPrefab_Hook(default));
                static GameObject Get_toothPrefab_Hook(GameObject toothPrefab)
                {
                    return IsEnabled() ? CraftData.GetPrefabForTechType(TryGetItemReplacement(TechType.StalkerTooth)) : toothPrefab;
                }
            }
        }

        [HarmonyPatch(typeof(Stillsuit), nameof(Stillsuit.UpdateEquipped))]
        static class Stillsuit_UpdateEquipped_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                FieldInfo Stillsuit_waterPrefab_FI = AccessTools.Field(typeof(Stillsuit), nameof(Stillsuit.waterPrefab));
                FieldInfo Eatable_waterValue_FI = AccessTools.Field(typeof(Eatable), nameof(Eatable.waterValue));
                MethodInfo Component_gameObject_get_MI = AccessTools.PropertyGetter(typeof(Component), nameof(Component.gameObject));

                LocalBuilder isLootRandomizerEnabled = generator.DeclareLocal(typeof(bool));
                yield return new CodeInstruction(OpCodes.Call, IsEnabled_MI);
                yield return new CodeInstruction(OpCodes.Stloc, isLootRandomizerEnabled);

                LocalBuilder replacementItemObj = generator.DeclareLocal(typeof(GameObject));
                yield return new CodeInstruction(OpCodes.Call, Hooks.GetReplacementItemPrefab_MI);
                yield return new CodeInstruction(OpCodes.Stloc, replacementItemObj);

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.LoadsField(Eatable_waterValue_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, isLootRandomizerEnabled);

                        Label originalInstr = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Brfalse_S, originalInstr);

                        yield return new CodeInstruction(OpCodes.Pop); // Pop instance
                        yield return new CodeInstruction(OpCodes.Ldc_R4, 20f);

                        Label afterPatch = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Br_S, afterPatch);

                        yield return instruction.WithLabels(originalInstr);

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(afterPatch);
                    }
                    else if (instruction.Calls(Component_gameObject_get_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldloc, isLootRandomizerEnabled);

                        Label skipInstruction = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Brtrue_S, skipInstruction);

                        Label skipPatch = generator.DefineLabel();
                        yield return instruction;
                        yield return new CodeInstruction(OpCodes.Br_S, skipPatch);

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipInstruction);
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldloc, replacementItemObj);

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipPatch);
                    }
                    else if (instruction.LoadsField(Stillsuit_waterPrefab_FI))
                    {
                        yield return instruction;

                        yield return new CodeInstruction(OpCodes.Ldloc, isLootRandomizerEnabled);

                        Label afterPatch = generator.DefineLabel();
                        yield return new CodeInstruction(OpCodes.Brfalse_S, afterPatch);

                        yield return new CodeInstruction(OpCodes.Pop); // Pop waterPrefab
                        yield return new CodeInstruction(OpCodes.Ldnull);

                        yield return new CodeInstruction(OpCodes.Nop).WithLabels(afterPatch);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetReplacementItemPrefab_MI = SymbolExtensions.GetMethodInfo(() => GetReplacementItemPrefab());
                static GameObject GetReplacementItemPrefab()
                {
                    return IsEnabled() ? CraftData.GetPrefabForTechType(TryGetItemReplacement(TechType.StillsuitWater)) : null;
                }
            }
        }

        [HarmonyPatch(typeof(BreakableResource), nameof(BreakableResource.SpawnResourceFromPrefab))]
        static class BreakableResource_SpawnResourceFromPrefab_Patch
        {
            static void Prefix(ref GameObject breakPrefab)
            {
                if (!IsEnabled())
                    return;

                TechType originalType = CraftData.GetTechType(breakPrefab);
                TechType replacementType = TryGetItemReplacement(originalType);
                breakPrefab = CraftData.GetPrefabForTechType(replacementType);
            }
        }

        [HarmonyPatch]
        static class GasoPod_DropGasPods_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<GasoPod>(_ => _.DropGasPods());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo Instantiate_GameObject_MI = SymbolExtensions.GetMethodInfo(() => GameObject.Instantiate<GameObject>(default(GameObject)));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(Instantiate_GameObject_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.onPodInstantiate_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo onPodInstantiate_MI = SymbolExtensions.GetMethodInfo(() => onPodInstantiate(default));
                static void onPodInstantiate(GameObject podObj)
                {
                    if (IsEnabled())
                    {
                        TechType newModelType = TryGetItemReplacement(TechType.GasPod);
                        if (newModelType != TechType.GasPod)
                        {
                            GasPod gasPod = podObj.GetComponent<GasPod>();
                            GameObject newModel = GameObject.Instantiate(CraftData.GetPrefabForTechType(newModelType), Vector3.zero, Quaternion.identity, false);

                            //newModel.EnsureComponent<Pickupable>();

                            newModel.PrepareStaticItem();
                            newModel.RemoveAllComponentsNotIn(gasPod.model);
                            newModel.SetRigidbodiesKinematic(true);

                            newModel.SetActive(true);

                            newModel.transform.SetParent(gasPod.model.transform.parent, true);
                            newModel.transform.localPosition = gasPod.model.transform.localPosition;
                            newModel.transform.localRotation = Utils.Random.Rotation;

                            if (newModel.TryGetModelBounds(out Bounds newModelBounds))
                            {
                                if (gasPod.model.TryGetModelBounds(out Bounds gasPodModelBounds))
                                {
                                    newModel.transform.position += gasPodModelBounds.center - newModelBounds.center;
                                }
                                else
                                {
                                    Utils.LogWarning($"Could not get model bounds for {nameof(TechType.GasPod)} ({gasPod.model.name})");
                                }
                            }
                            else
                            {
                                Utils.LogWarning($"Could not get model bounds for {newModelType} ({newModel.name})");
                            }

                            GameObject.Destroy(gasPod.model);
                            gasPod.model = newModel;
                        }
                    }
                }
            }
        }

        //[HarmonyPatch]
        static class CrashHome_Start_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<CrashHome>(_ => _.Start());
            }

            static void Prefix(CrashHome __instance)
            {
                if (!IsEnabled())
                    return;

                PrefabPlaceholder prefabPlaceholder = __instance.GetComponentInChildren<PrefabPlaceholder>();
                if (prefabPlaceholder.Exists())
                {
                    CraftData.PreparePrefabIDCache();
                    if (CraftData.entClassTechTable.TryGetValue(prefabPlaceholder.prefabClassId, out TechType techType))
                    {
                        prefabPlaceholder.prefabClassId = CraftData.GetClassIdForTechType(TryGetItemReplacement(techType));
                    }
                }
            }
        }
    }
}
