using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GRandomizer.MiscPatches
{
    [HarmonyPatch]
    [HarmonyPriority(1)] // Patch before the TechType replacing patch (like LootRandomizer)
    static class EggPatch
    {
        // This is fucking dumb. Who at UWE thought "haha yes when you discover an egg it becomes a different item, this is a great idea"
        static readonly DualDictionary<TechType, TechType> _eggPairs = new DualDictionary<TechType, TechType>
        {
            ( TechType.BonesharkEgg, TechType.BonesharkEggUndiscovered ),
            ( TechType.CrabsnakeEgg, TechType.CrabsnakeEggUndiscovered ),
            ( TechType.CrabsquidEgg, TechType.CrabsquidEggUndiscovered ),
            ( TechType.CrashEgg, TechType.CrashEggUndiscovered ),
            ( TechType.CutefishEgg, TechType.CutefishEggUndiscovered ),
            ( TechType.GasopodEgg, TechType.GasopodEggUndiscovered ),
            ( TechType.JellyrayEgg, TechType.JellyrayEggUndiscovered ),
            ( TechType.JumperEgg, TechType.JumperEggUndiscovered ),
            ( TechType.LavaLizardEgg, TechType.LavaLizardEggUndiscovered ),
            ( TechType.MesmerEgg, TechType.MesmerEggUndiscovered ),
            ( TechType.RabbitrayEgg, TechType.RabbitrayEggUndiscovered ),
            ( TechType.ReefbackEgg, TechType.ReefbackEggUndiscovered ),
            ( TechType.SandsharkEgg, TechType.SandsharkEggUndiscovered ),
            ( TechType.ShockerEgg, TechType.ShockerEggUndiscovered ),
            ( TechType.SpadefishEgg, TechType.SpadefishEggUndiscovered ),
            ( TechType.StalkerEgg, TechType.StalkerEggUndiscovered )
        };

        public static readonly MethodInfo CorrectEggType_MI = SymbolExtensions.GetMethodInfo(() => CorrectEggType(default));
        public static TechType CorrectEggType(TechType eggType)
        {
            if (_eggPairs.F2S_TryGetValue(eggType, out TechType undiscoveredEgg))
            {
                if (!KnownTech.Contains(eggType))
                    return undiscoveredEgg;
            }
            else if (_eggPairs.S2F_TryGetValue(eggType, out TechType discoveredEgg))
            {
                if (KnownTech.Contains(discoveredEgg))
                    return discoveredEgg;
            }

            return eggType;
        }

        public static TechType ToDiscoveredEggType(TechType eggType)
        {
            return _eggPairs.S2F_TryGetValue(eggType, out TechType discoveredEgg) ? discoveredEgg : eggType;
        }

        static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return SymbolExtensions.GetMethodInfo(() => CraftData.GetCookedData(default));
            yield return SymbolExtensions.GetMethodInfo(() => CraftData.GetHarvestOutputData(default));

            foreach (MethodInfo get_techType in typeof(IIngredient).GetImplementations(false, AccessTools.PropertyGetter(typeof(IIngredient), nameof(IIngredient.techType))))
            {
                yield return get_techType;
            }

            yield return SymbolExtensions.GetMethodInfo(() => CrafterLogic.IsCraftRecipeUnlocked(default));
            yield return SymbolExtensions.GetMethodInfo(() => CrafterLogic.IsCraftRecipeFulfilled(default));

            yield return SymbolExtensions.GetMethodInfo<Inventory>(_ => _.ConsumeResourcesForRecipe(default, default));
            yield return SymbolExtensions.GetMethodInfo<Inventory>(_ => _.GetPickupCount(default));

            yield return SymbolExtensions.GetMethodInfo<Constructable>(_ => _.Construct());

            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.Contains(default(TechType)));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.Contains(default(InventoryItem)));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.Contains(default(Pickupable)));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.DestroyItem(default));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.GetCount(default));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.GetItems(default));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.GetItems(default, default));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.RemoveItem(default));
            yield return SymbolExtensions.GetMethodInfo<ItemsContainer>(_ => _.RemoveItem(default, default));

            yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.Start());
            yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.UpdateModel());
            yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterSalt());
            yield return SymbolExtensions.GetMethodInfo<FiltrationMachine>(_ => _.TryFilterWater());
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase patchedMethod, ILGenerator generator)
        {
            LocalGenerator localGen = new LocalGenerator(generator);

            bool returnsTechType = patchedMethod is MethodInfo patchedMethodInfo && patchedMethodInfo.ReturnType == typeof(TechType);

            foreach (CodeInstruction instruction in instructions)
            {
                if ((instruction.opcode == OpCodes.Stfld || instruction.opcode == OpCodes.Stsfld) && instruction.operand is FieldInfo field)
                {
                    if (field.FieldType == typeof(TechType))
                        yield return new CodeInstruction(OpCodes.Call, CorrectEggType_MI);
                }
                else if ((instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Newobj) && instruction.operand is MethodBase method)
                {
                    Type[] parameters = method.GetParameters().Select(p => p.ParameterType).ToArray();
                    int firstTechTypeIndex = Array.IndexOf(parameters, typeof(TechType));
                    if (parameters.Any(p => p == typeof(TechType)))
                    {
                        LocalBuilder[] parameterLocals = new LocalBuilder[parameters.Length];

                        // Last TechType argument doesn't need a local, just keep it on the stack
                        for (int i = parameters.Length - 1; i >= firstTechTypeIndex + 1; i--)
                        {
                            LocalBuilder local = localGen.GetLocal(parameters[i], false);

                            // If there are multiple TechType arguments, correct them before storing the local
                            if (parameters[i] == typeof(TechType))
                                yield return new CodeInstruction(OpCodes.Call, CorrectEggType_MI);

                            yield return new CodeInstruction(OpCodes.Stloc, local);
                            parameterLocals[i] = local;
                        }

                        // Correct the first TechType argument that was left on the stack
                        yield return new CodeInstruction(OpCodes.Call, CorrectEggType_MI);

                        for (int i = firstTechTypeIndex + 1; i < parameters.Length; i++)
                        {
                            yield return new CodeInstruction(OpCodes.Ldloc, parameterLocals[i]);
                            localGen.ReleaseLocal(parameterLocals[i]);
                        }
                    }
                }
                else if (returnsTechType && instruction.opcode == OpCodes.Ret)
                {
                    yield return new CodeInstruction(OpCodes.Call, CorrectEggType_MI);
                }

                yield return instruction;
            }
        }
    }
}
