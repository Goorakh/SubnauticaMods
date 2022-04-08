using GRandomizer.Util;
using HarmonyLib;
using QModManager.Utility;
using SMLHelper.V2.Handlers;
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
    class LootRandomizer : AutoSingleton<LootRandomizer>, IRandomizerController
    {
        // TODO: Find a reliable way to automatically filter item types at runtime to include modded items aswell
        static readonly TechType[] _standardItemTypes = new TechType[]
        {
            TechType.AcidMushroom,
            TechType.AdvancedWiringKit,
            TechType.Aerogel,
            TechType.AirBladder,
            TechType.AluminumOxide,
            TechType.AramidFibers,
            TechType.ArcadeGorgetoy,
            TechType.Battery,
            TechType.Beacon,
            TechType.Benzene,
            TechType.BigFilteredWater,
            TechType.Bladderfish,
            TechType.Bleach,
            TechType.BloodOil,
            TechType.BonesharkEgg,
            TechType.Boomerang,
            TechType.Builder,
            TechType.BulboTreePiece,
            TechType.Cap1,
            TechType.Cap2,
            TechType.Coffee,
            TechType.CoffeeVendingMachine,
            TechType.Compass,
            TechType.ComputerChip,
            TechType.Constructor,
            TechType.CookedBladderfish,
            TechType.CookedBoomerang,
            TechType.CookedEyeye,
            TechType.CookedGarryFish,
            TechType.CookedHoleFish,
            TechType.CookedHoopfish,
            TechType.CookedHoverfish,
            TechType.CookedLavaBoomerang,
            TechType.CookedLavaEyeye,
            TechType.CookedOculus,
            TechType.CookedPeeper,
            TechType.CookedReginald,
            TechType.CookedSpadefish,
            TechType.CookedSpinefish,
            TechType.Copper,
            TechType.CopperWire,
            TechType.CoralChunk,
            TechType.CrabsnakeEgg,
            TechType.CrabsquidEgg,
            TechType.CrashEgg,
            TechType.CrashPowder,
            TechType.CreepvinePiece,
            TechType.CreepvineSeedCluster,
            TechType.CuredBladderfish,
            TechType.CuredBoomerang,
            TechType.CuredEyeye,
            TechType.CuredGarryFish,
            TechType.CuredHoleFish,
            TechType.CuredHoopfish,
            TechType.CuredHoverfish,
            TechType.CuredLavaBoomerang,
            TechType.CuredLavaEyeye,
            TechType.CuredOculus,
            TechType.CuredPeeper,
            TechType.CuredReginald,
            TechType.CuredSpadefish,
            TechType.CuredSpinefish,
            TechType.CutefishEgg,
            TechType.CyclopsDecoy,
            TechType.CyclopsDecoyModule,
            TechType.CyclopsFireSuppressionModule,
            TechType.CyclopsHullModule1,
            TechType.CyclopsHullModule2,
            TechType.CyclopsHullModule3,
            TechType.CyclopsSeamothRepairModule,
            TechType.CyclopsShieldModule,
            TechType.CyclopsSonarModule,
            TechType.CyclopsThermalReactorModule,
            TechType.DepletedReactorRod,
            TechType.Diamond,
            TechType.DisinfectedWater,
            TechType.DiveReel,
            TechType.DiveSuit,
            TechType.DoubleTank,
            TechType.EnameledGlass,
            TechType.ExoHullModule1,
            TechType.ExoHullModule2,
            TechType.ExosuitDrillArmModule,
            TechType.ExosuitGrapplingArmModule,
            TechType.ExosuitJetUpgradeModule,
            TechType.ExosuitPropulsionArmModule,
            TechType.ExosuitThermalReactorModule,
            TechType.ExosuitTorpedoArmModule,
            TechType.EyesPlantSeed,
            TechType.Eyeye,
            TechType.FernPalmSeed,
            TechType.FiberMesh,
            TechType.FilteredWater,
            TechType.Fins,
            TechType.FireExtinguisher,
            TechType.FirstAidKit,
            TechType.Flare,
            TechType.Flashlight,
            TechType.Floater,
            TechType.GabeSFeatherSeed,
            TechType.GarryFish,
            TechType.GasopodEgg,
            TechType.GasPod,
            TechType.GasTorpedo,
            TechType.Glass,
            TechType.Gold,
            TechType.Gravsphere,
            TechType.HangingFruit,
            TechType.HeatBlade,
            TechType.HighCapacityTank,
            TechType.HoleFish,
            TechType.Hoopfish,
            TechType.Hoverfish,
            TechType.HullReinforcementModule,
            TechType.HullReinforcementModule2,
            TechType.HullReinforcementModule3,
            TechType.HydrochloricAcid,
            TechType.JellyPlant,
            TechType.JellyPlantSeed,
            TechType.JellyrayEgg,
            TechType.JeweledDiskPiece,
            TechType.JumperEgg,
            TechType.Knife,
            TechType.KooshChunk,
            TechType.Kyanite,
            TechType.LabContainer,
            TechType.LabContainer2,
            TechType.LabContainer3,
            TechType.LabEquipment1,
            TechType.LabEquipment2,
            TechType.LabEquipment3,
            TechType.LaserCutter,
            TechType.LavaBoomerang,
            TechType.LavaEyeye,
            TechType.LavaLizardEgg,
            TechType.Lead,
            TechType.LEDLight,
            TechType.Lithium,
            TechType.Lubricant,
            TechType.LuggageBag,
            TechType.Magnetite,
            TechType.MapRoomCamera,
            TechType.MapRoomHUDChip,
            TechType.MapRoomUpgradeScanRange,
            TechType.MapRoomUpgradeScanSpeed,
            TechType.Melon,
            TechType.MelonSeed,
            TechType.MembrainTreeSeed,
            TechType.MesmerEgg,
            TechType.Nickel,
            TechType.NutrientBlock,
            TechType.Oculus,
            TechType.OrangePetalsPlantSeed,
            TechType.Peeper,
            TechType.PinkFlowerSeed,
            TechType.Pipe,
            TechType.PipeSurfaceFloater,
            TechType.PlasteelIngot,
            TechType.PlasteelTank,
            TechType.Polyaniline,
            TechType.Poster,
            TechType.PosterAurora,
            TechType.PosterExoSuit1,
            TechType.PosterExoSuit2,
            TechType.PosterKitty,
            TechType.PowerCell,
            TechType.PrecursorIonBattery,
            TechType.PrecursorIonCrystal,
            TechType.PrecursorIonPowerCell,
            TechType.PrecursorKey_Blue,
            TechType.PrecursorKey_Orange,
            TechType.PrecursorKey_Purple,
            TechType.PropulsionCannon,
            TechType.PurpleBrainCoralPiece,
            TechType.PurpleBranchesSeed,
            TechType.PurpleFanSeed,
            TechType.PurpleRattleSpore,
            TechType.PurpleStalkSeed,
            TechType.PurpleTentacleSeed,
            TechType.PurpleVasePlantSeed,
            TechType.PurpleVegetable,
            TechType.Quartz,
            TechType.RabbitrayEgg,
            TechType.RadiationGloves,
            TechType.RadiationHelmet,
            TechType.RadiationSuit,
            TechType.ReactorRod,
            TechType.Rebreather,
            TechType.RedBasketPlantSeed,
            TechType.RedBushSeed,
            TechType.RedConePlantSeed,
            TechType.RedGreenTentacleSeed,
            TechType.RedRollPlantSeed,
            TechType.ReefbackEgg,
            TechType.Reginald,
            TechType.ReinforcedDiveSuit,
            TechType.ReinforcedGloves,
            TechType.RepulsionCannon,
            TechType.Salt,
            TechType.SandsharkEgg,
            TechType.Scanner,
            TechType.ScrapMetal,
            TechType.SeaCrownSeed,
            TechType.Seaglide,
            TechType.SeamothElectricalDefense,
            TechType.SeamothReinforcementModule,
            TechType.SeamothSolarCharge,
            TechType.SeamothSonarModule,
            TechType.SeamothTorpedoModule,
            TechType.SeaTreaderPoop,
            TechType.ShellGrassSeed,
            TechType.ShockerEgg,
            TechType.Silicone,
            TechType.Silver,
            TechType.SmallFanSeed,
            TechType.SmallMelon,
            TechType.Snack1,
            TechType.Snack2,
            TechType.Snack3,
            TechType.SnakeMushroomSpore,
            TechType.Spadefish,
            TechType.SpadefishEgg,
            TechType.SpikePlantSeed,
            TechType.Spinefish,
            TechType.SpottedLeavesPlantSeed,
            TechType.StalkerEgg,
            TechType.StalkerTooth,
            TechType.StarshipSouvenir,
            TechType.StasisRifle,
            TechType.Stillsuit,
            TechType.StillsuitWater,
            TechType.Sulphur,
            TechType.SwimChargeFins,
            TechType.Tank,
            TechType.Thermometer,
            TechType.Titanium,
            TechType.TitaniumIngot,
            TechType.ToyCar,
            TechType.TreeMushroomPiece,
            TechType.UltraGlideFins,
            TechType.UraniniteCrystal,
            TechType.VehicleArmorPlating,
            TechType.VehicleHullModule1,
            TechType.VehicleHullModule2,
            TechType.VehicleHullModule3,
            TechType.VehiclePowerUpgradeModule,
            TechType.VehicleStorageModule,
            TechType.Welder,
            TechType.WiringKit
        };
        static TechType[] _currentItemTypes;
        static TechType[] itemTypes
        {
            get
            {
                if (_currentItemTypes == null)
                {
                    // TODO: Filter this against a configurable blacklist
                    // TODO: Add types from user config
                    _currentItemTypes = _standardItemTypes;
                }

                return _currentItemTypes;
            }
        }

        static readonly Dictionary<TechType, TechType> _itemReplacementsDictionary = new Dictionary<TechType, TechType>();

        static readonly MethodInfo tryGetItemReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetItemReplacement(default));
        static TechType tryGetItemReplacement(TechType techType)
        {
            if (!Instance.IsEnabled())
                return techType;

            if (_itemReplacementsDictionary.TryGetValue(techType, out TechType replacement))
            {
                return replacement;
            }
            else
            {
                TechType replacementType = itemTypes.GetRandom();
#if DEBUG
                Utils.DebugLog($"Replace item: {techType}->{replacementType}");
#endif
                return _itemReplacementsDictionary[techType] = replacementType;
            }
        }
        static void tryReplaceItem(ref TechType techType)
        {
            techType = tryGetItemReplacement(techType);
        }

        public bool IsEnabled()
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
                if (Mod.Config.RandomLoot)
                {
                    TechType[] result = new TechType[Mathf.Max(__result.Length + UnityEngine.Random.Range(-2, 3), 1)];
                    for (int i = 0; i < result.Length; i++)
                    {
                        result[i] = itemTypes.GetRandom();
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
        static class InstantiateLoot_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<SpawnEscapePodSupplies>(_ => _.OnNewBorn());
                yield return SymbolExtensions.GetMethodInfo<SpawnStoredLoot>(_ => _.SpawnRandomStoredItems());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo GameObject_GetComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.GetComponent<Pickupable>());
                MethodInfo GameObject_GetOrAddComponent_Pickupable_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.GetOrAddComponent<Pickupable>());

                return instructions.MethodReplacer(GameObject_GetComponent_Pickupable_MI, GameObject_GetOrAddComponent_Pickupable_MI);
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

        //[HarmonyPatch]
        static class AddToInventory_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<BlueprintHandTarget>(_ => _.DisableGameObjectAsync()));
                yield return SymbolExtensions.GetMethodInfo<BlueprintHandTarget>(_ => _.UnlockBlueprint());
                yield return SymbolExtensions.GetMethodInfo<CoffeeVendingMachine>(_ => _.SpawnCoffee());
                yield return SymbolExtensions.GetMethodInfo<Creepvine>(_ => _.OnKnifeHit(default));
                yield return SymbolExtensions.GetMethodInfo<IntroFireExtinguisherHandTarget>(_ => _.UseVolume());
                yield return SymbolExtensions.GetMethodInfo<CoffeeVendingMachine>(_ => _.SpawnCoffee());
                yield return SymbolExtensions.GetMethodInfo<CoffeeVendingMachine>(_ => _.SpawnCoffee());
            }


        }
    }
}
