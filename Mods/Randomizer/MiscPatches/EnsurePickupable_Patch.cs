using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.MiscPatches
{
    [HarmonyPatch]
    static class EnsurePickupable_Patch
    {
        static IEnumerable<MethodInfo> TargetMethods()
        {
            yield return SymbolExtensions.GetMethodInfo<SpawnEscapePodSupplies>(_ => _.OnNewBorn());
            yield return SymbolExtensions.GetMethodInfo<SpawnStoredLoot>(_ => _.SpawnRandomStoredItems());
            yield return SymbolExtensions.GetMethodInfo<Stillsuit>(_ => _.UpdateEquipped(default, default));
            yield return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo(() => CraftData.AddToInventoryRoutine(default, default, default, default, default)));
            yield return SymbolExtensions.GetMethodInfo<Pickupable>(_ => _.Initialize());
            yield return SymbolExtensions.GetMethodInfo<CrafterLogic>(_ => _.TryPickupSingle(default));
            yield return SymbolExtensions.GetMethodInfo(() => CrafterGhostModel.GetGhostModel(default));
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
                return component.Exists() ? component : obj.AddComponent<Pickupable>();
            }
        }
    }
}
