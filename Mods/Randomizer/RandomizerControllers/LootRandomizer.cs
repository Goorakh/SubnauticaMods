using GRandomizer.Util;
using HarmonyLib;
using QModManager.Utility;
using SMLHelper.V2.Handlers;
using Story;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class LootRandomizer
    {
        // WHY ARE THESE SEPARATE TECHTYPES????!?!??!?!?!?!?!!?!
        static readonly Dictionary<TechType, TechType> _eggToUndiscoveredEgg = new Dictionary<TechType, TechType>
        {
            { TechType.BonesharkEgg, TechType.BonesharkEggUndiscovered },
            { TechType.CrabsnakeEgg, TechType.CrabsnakeEggUndiscovered },
            { TechType.CrabsquidEgg, TechType.CrabsquidEggUndiscovered },
            { TechType.CrashEgg, TechType.CrashEggUndiscovered },
            { TechType.CutefishEgg, TechType.CutefishEggUndiscovered },
            { TechType.GasopodEgg, TechType.GasopodEggUndiscovered },
            { TechType.JellyrayEgg, TechType.JellyrayEggUndiscovered },
            { TechType.JumperEgg, TechType.JumperEggUndiscovered },
            { TechType.LavaLizardEgg, TechType.LavaLizardEggUndiscovered },
            { TechType.MesmerEgg, TechType.MesmerEggUndiscovered },
            { TechType.RabbitrayEgg, TechType.RabbitrayEggUndiscovered },
            { TechType.ReefbackEgg, TechType.ReefbackEggUndiscovered },
            { TechType.SandsharkEgg, TechType.SandsharkEggUndiscovered },
            { TechType.ShockerEgg, TechType.ShockerEggUndiscovered },
            { TechType.SpadefishEgg, TechType.SpadefishEggUndiscovered },
            { TechType.StalkerEgg, TechType.StalkerEggUndiscovered }
        };

        static TechType[] _currentItemTypes;
        static TechType[] itemTypes
        {
            get
            {
                if (_currentItemTypes == null)
                {
                    HashSet<TechType> obtainableTypes = (from TechType groupType in
                                                         from itemGroup in CraftData.groups
                                                         where itemGroup.key != TechGroup.Constructor // Exclude vehicles in mobile vehicle bay
                                                         where itemGroup.key != TechGroup.BasePieces // Exclude base pieces
                                                         where itemGroup.key != TechGroup.ExteriorModules // Exclude base pieces
                                                         where itemGroup.key != TechGroup.InteriorPieces // Exclude base pieces
                                                         where itemGroup.key != TechGroup.InteriorModules // Exclude base pieces
                                                         where itemGroup.key != TechGroup.Miscellaneous // Exclude base pieces
                                                         from subGroup in itemGroup.Value
                                                         where subGroup.key != TechCategory.Cyclops // Exclude cyclops blueprints
                                                         from techType in subGroup.Value
                                                         select techType
                                                         select groupType).ToHashSet();

                    foreach (TechType type in new HashSet<TechType>(obtainableTypes)) // Clone collection since it will be modified in the foreach
                    {
                        if (CraftData.techData.TryGetValue(type, out CraftData.TechData data))
                        {
                            if (data._linkedItems != null)
                            {
                                foreach (TechType linked in data._linkedItems)
                                {
                                    obtainableTypes.Add(linked);
                                }
                            }

                            if (data._ingredients != null)
                            {
                                foreach (CraftData.Ingredient ingredient in data._ingredients)
                                {
                                    if (ingredient != null)
                                    {
                                        obtainableTypes.Add(ingredient._techType);
                                    }
                                }
                            }
                        }
                    }

                    foreach (string includeStr in ConfigReader.ReadFromFile<string[]>("Randomizers/ItemRandomizer::Include"))
                    {
                        if (TechTypeExtensions.FromString(includeStr, out TechType includeType, true))
                        {
                            obtainableTypes.Add(includeType);
                        }
                        else
                        {
                            Utils.LogWarning($"Unknown TechType ({includeStr}) in ItemRandomizer.json Include list (are you missing a mod?)", false);
                        }
                    }

                    foreach (string excludeStr in ConfigReader.ReadFromFile<string[]>("Randomizers/ItemRandomizer::Blacklist"))
                    {
                        if (TechTypeExtensions.FromString(excludeStr, out TechType excludeType, true))
                        {
                            obtainableTypes.Remove(excludeType);
                        }
                        else
                        {
                            Utils.LogWarning($"Unknown TechType ({excludeStr}) in ItemRandomizer.json Blacklist (are you missing a mod?)", false);
                        }
                    }

                    foreach (TechType type in new HashSet<TechType>(obtainableTypes)) // Clone collection since it will be modified in the foreach
                    {
                        if (CraftData.GetPrefabForTechType(type) == null)
                        {
                            Utils.LogWarning($"Removing item type {type} due to no prefab defined in CraftData", true);
                            obtainableTypes.Remove(type);
                        }
                    }

                    _currentItemTypes = obtainableTypes.ToArray();
                }

                return _currentItemTypes;
            }
        }

        static readonly Dictionary<TechType, TechType> _itemReplacementsDictionary = new Dictionary<TechType, TechType>();

        static readonly MethodInfo tryGetItemReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetItemReplacement(default));
        static TechType tryGetItemReplacement(TechType techType)
        {
            return tryGetItemReplacement(techType, null);
        }

#if DEBUG
        static int _debugIndex = 0;
        public static void IncreaseDebugIndex()
        {
            if (++_debugIndex >= itemTypes.Length)
                _debugIndex = 0;

            Utils.DebugLog($"_debugIndex: {_debugIndex} ({itemTypes[_debugIndex]})", true);
        }
        public static void DecreaseDebugIndex()
        {
            if (--_debugIndex < 0)
                _debugIndex = itemTypes.Length - 1;

            Utils.DebugLog($"_debugIndex: {_debugIndex} ({itemTypes[_debugIndex]})", true);
        }
#endif

        static TechType tryGetItemReplacement(TechType techType, Predicate<TechType> condition)
        {
            if (!IsEnabled())
                return techType;

#if DEBUG
            if (false && techType == TechType.BigFilteredWater)
            {
                return itemTypes[_debugIndex];
            }
#endif

            if (_itemReplacementsDictionary.TryGetValue(techType, out TechType replacement))
            {
                return replacement;
            }
            else
            {
                if (!itemTypes.Contains(techType))
                    return _itemReplacementsDictionary[techType] = techType;

                TechType replacementType;
                do
                {
                    replacementType = itemTypes.GetRandom();
                } while (replacementType == techType || (condition != null && !condition(replacementType)));

#if DEBUG
                Utils.DebugLog($"Replace item: {techType} -> {replacementType}", false);
#endif

                return _itemReplacementsDictionary[techType] = replacementType;
            }
        }
        static void tryReplaceItem(ref TechType techType)
        {
            techType = tryGetItemReplacement(techType);
        }

        static bool IsEnabled()
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
                        result[i] = i < __result.Length ? tryGetItemReplacement(__result[i]) : itemTypes.GetRandom();
                    }

                    return result;
                }
                else
                {
                    return __result;
                }
            }
        }

        [HarmonyPatch]
        static class EnsurePickupable_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<SpawnEscapePodSupplies>(_ => _.OnNewBorn());
                yield return SymbolExtensions.GetMethodInfo<SpawnStoredLoot>(_ => _.SpawnRandomStoredItems());
                yield return SymbolExtensions.GetMethodInfo<Stillsuit>(_ => _.UpdateEquipped(default, default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo GameObject_GetComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.GetComponent<Pickupable>());

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(GameObject_GetComponent_Pickupable_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // Dup instance

                        yield return instruction;

                        yield return new CodeInstruction(OpCodes.Call, Hooks.AddComponentIfNeeded_MI);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo AddComponentIfNeeded_MI = SymbolExtensions.GetMethodInfo(() => AddComponentIfNeeded(default, default));
                static Pickupable AddComponentIfNeeded(GameObject obj, Pickupable component)
                {
                    return component ?? obj.AddComponent<Pickupable>();
                }
            }
        }

        [HarmonyPatch(typeof(PickPrefab), nameof(PickPrefab.Start))]
        static class PickPrefab_Start_Patch
        {
            static void Prefix(PickPrefab __instance)
            {
                tryReplaceItem(ref __instance.pickTech);
            }
        }

        static IEnumerable<CodeInstruction> hookTechType(IEnumerable<CodeInstruction> instructions, int type)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;

                if (instruction.LoadsConstant(type))
                {
                    yield return new CodeInstruction(OpCodes.Call, tryGetItemReplacement_MI);
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

                        yield return new CodeInstruction(OpCodes.Call, tryGetItemReplacement_MI); // Get replacement TechType

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
                return tryGetItemReplacement(__result);
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

                    for (int i = 0; i < renderers.Length; i++)
                    {
                        GameObject itemModel = CraftData.InstantiateFromPrefab(techType);
                        Utils.PrepareStaticItem(itemModel);
                        itemModel.RemoveAllComponentsNotIn(renderers[i].gameObject);

                        itemModel.transform.SetParent(renderers[i].transform.parent);
                        itemModel.transform.localRotation = Utils.Random.Rotation;

                        // Don't blind the player with 10 light sources in the same spot
                        foreach (Light light in itemModel.GetComponentsInChildren<Light>())
                        {
                            light.enabled = false;
                        }

                        if (itemModel.TryGetModelBounds(out Bounds modelBounds) && renderers[i].gameObject.TryGetModelBounds(out Bounds rendererBounds))
                        {
                            itemModel.transform.position += rendererBounds.center - modelBounds.center;

                            float scaleMult = rendererBounds.size.magnitude / modelBounds.size.magnitude;
                            if (scaleMult > 1.5f || scaleMult < (1 / 3f))
                                itemModel.transform.localScale *= scaleMult;

                            if (itemModel.GetComponentInChildren<Collider>() == null)
                            {
                                BoxCollider boxCollider = itemModel.AddComponent<BoxCollider>();
                                boxCollider.size = modelBounds.size;
                                boxCollider.center = modelBounds.center;
                            }
                        }
                        else
                        {
                            Utils.LogWarning($"[replaceDrillableChunks]: Could not get model bounds for {techType} ({itemModel.name}) or renderers[{i}] ({renderers[i].name})", true);

                            itemModel.transform.localPosition = renderers[i].transform.localPosition;
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
                    ItemsContainer container = __instance.storageContainer.container;
                    int containerSizeX = container.sizeX;
                    int containerSizeY = container.sizeY;

                    TechType newType = tryGetItemReplacement(prefab.GetTechType(), t =>
                    {
                        Vector2int size = CraftData.GetItemSize(t);
                        return size.x <= containerSizeX && size.y <= containerSizeY;
                    });

                    GameObject newPrefab = CraftData.GetPrefabForTechType(newType);
                    Pickupable pickupablePrefab = newPrefab.GetComponent<Pickupable>();
                    if (pickupablePrefab != null)
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
            static GameObject _overrideSaltModel;
            static GameObject overrideSaltModel
            {
                get
                {
                    if (_overrideSaltModel == null)
                        _overrideSaltModel = CraftData.GetPrefabForTechType(tryGetItemReplacement(TechType.Salt));

                    return _overrideSaltModel;
                }
            }

            static GameObject _overrideWaterModel;
            static GameObject overrideWaterModel
            {
                get
                {
                    if (_overrideWaterModel == null)
                        _overrideWaterModel = CraftData.GetPrefabForTechType(tryGetItemReplacement(TechType.BigFilteredWater));

                    return _overrideWaterModel;
                }
            }

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
                    foreach (CodeInstruction instruction in instructions)
                    {
                        yield return instruction;

                        if (instruction.LoadsField(FiltrationMachine_saltModel_FI))
                        {
                            yield return new CodeInstruction(OpCodes.Call, Hooks.Get_saltModel_Hook_MI);
                        }
                        else if (instruction.LoadsField(FiltrationMachine_waterModel_FI))
                        {
                            yield return new CodeInstruction(OpCodes.Call, Hooks.Get_waterModel_Hook_MI);
                        }
                    }
                }

                static class Hooks
                {
                    public static readonly MethodInfo Get_saltModel_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_saltModel_Hook(default));
                    static GameObject Get_saltModel_Hook(GameObject saltModel)
                    {
                        return IsEnabled() ? overrideSaltModel : saltModel;
                    }

                    public static readonly MethodInfo Get_waterModel_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_waterModel_Hook(default));
                    static GameObject Get_waterModel_Hook(GameObject waterModel)
                    {
                        return IsEnabled() ? overrideWaterModel : waterModel;
                    }
                }
            }

            [HarmonyPatch]
            static class FiltrationMachine_EggPatch
            {
                static IEnumerable<MethodInfo> TargetMethods()
                {
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.Start());
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.UpdateModel());
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterSalt());
                    yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterWater());
                }

                static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
                {
                    LocalBuilder techTypeLoc = generator.DeclareLocal(typeof(TechType));
                    LocalBuilder countLoc = generator.DeclareLocal(typeof(int));

                    MethodInfo ItemsContainer_GetCount_MI = SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.GetCount(default));

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (instruction.Calls(ItemsContainer_GetCount_MI))
                        {
                            yield return new CodeInstruction(OpCodes.Stloc, techTypeLoc);
                            yield return new CodeInstruction(OpCodes.Dup); // Dup instance

                            yield return new CodeInstruction(OpCodes.Ldloc, techTypeLoc);

                            yield return instruction;
                            yield return new CodeInstruction(OpCodes.Stloc, countLoc);

                            yield return new CodeInstruction(OpCodes.Ldloc, countLoc);

                            Label skipEggCheck = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Brtrue, skipEggCheck);

                            yield return new CodeInstruction(OpCodes.Ldloc, techTypeLoc);
                            yield return new CodeInstruction(OpCodes.Call, Hooks.tryGetUndiscoveredEggType_MI);

                            yield return instruction;
                            yield return new CodeInstruction(OpCodes.Stloc, countLoc);

                            Label skipPop = generator.DefineLabel();
                            yield return new CodeInstruction(OpCodes.Br, skipPop);

                            // Pop instance
                            yield return new CodeInstruction(OpCodes.Pop).WithLabels(skipEggCheck);

                            yield return new CodeInstruction(OpCodes.Nop).WithLabels(skipPop);

                            yield return new CodeInstruction(OpCodes.Ldloc, countLoc);
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }

                static class Hooks
                {
                    public static readonly MethodInfo tryGetUndiscoveredEggType_MI = SymbolExtensions.GetMethodInfo(() => tryGetUndiscoveredEggType(default));
                    static TechType tryGetUndiscoveredEggType(TechType eggType)
                    {
                        if (IsEnabled() && _eggToUndiscoveredEgg.TryGetValue(eggType, out TechType undiscoveredType))
                            return undiscoveredType;

                        return eggType;
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
                    Utils.PrepareStaticItem(__instance.shownModel);
                    __instance.shownModel.RemoveAllComponentsNotIn(__instance.waterModel);

                    if (__instance.shownModel.GetComponentInChildren<VFXFabricating>() == null)
                    {
                        VFXFabricating fabricating = __instance.shownModel.AddComponent<VFXFabricating>();

                        if (__instance.shownModel.TryGetModelBounds(out Bounds modelBounds))
                        {
                            Vector3 center = modelBounds.center;

                            Vector3 halfHeight = new Vector3(0f, modelBounds.size.y / 2f, 0f);
                            Vector3 bottomCenter = center - halfHeight;
                            Vector3 topCenter = center + halfHeight;

                            fabricating.localMinY = fabricating.transform.InverseTransformPoint(bottomCenter).y;
                            fabricating.localMaxY = fabricating.transform.InverseTransformPoint(topCenter).y;
                        }
                        else
                        {
                            fabricating.localMinY = 0f;
                            fabricating.localMaxY = 1f;
                        }
                    }
                }

                static class Hooks
                {
                    public static readonly MethodInfo GetOrAddComponent_MI = SymbolExtensions.GetMethodInfo(() => GetComponent_Hook(default, default));
                    static VFXScan GetComponent_Hook(GameObject shownModel, VFXScan vfxComponent)
                    {
                        return vfxComponent ?? shownModel.AddComponent<VFXScan>();
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

                        yield return new CodeInstruction(OpCodes.Call, tryGetItemReplacement_MI); // Get replacement TechType

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
                tryReplaceItem(ref __result);
            }
        }

        [HarmonyPatch(typeof(Stalker), nameof(Stalker.LoseTooth))]
        static class Stalker_LoseTooth_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo Stalker_toothPrefab_FI = AccessTools.Field(typeof(Stalker), nameof(Stalker.toothPrefab));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.LoadsField(Stalker_toothPrefab_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, Hooks.Get_toothPrefab_Hook_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Get_toothPrefab_Hook_MI = SymbolExtensions.GetMethodInfo(() => Get_toothPrefab_Hook(default));
                static GameObject Get_toothPrefab_Hook(GameObject toothPrefab)
                {
                    return IsEnabled() ? CraftData.GetPrefabForTechType(tryGetItemReplacement(TechType.StalkerTooth)) : toothPrefab;
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
                yield return new CodeInstruction(OpCodes.Call, Hooks.IsLootRandomizerEnabled_MI);
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
                public static readonly MethodInfo IsLootRandomizerEnabled_MI = SymbolExtensions.GetMethodInfo(() => IsLootRandomizerEnabled());
                static bool IsLootRandomizerEnabled()
                {
                    return IsEnabled();
                }

                public static readonly MethodInfo GetReplacementItemPrefab_MI = SymbolExtensions.GetMethodInfo(() => GetReplacementItemPrefab());
                static GameObject GetReplacementItemPrefab()
                {
                    if (!IsLootRandomizerEnabled())
                        return null;

                    return CraftData.GetPrefabForTechType(tryGetItemReplacement(TechType.StillsuitWater));
                }
            }
        }
    }
}
