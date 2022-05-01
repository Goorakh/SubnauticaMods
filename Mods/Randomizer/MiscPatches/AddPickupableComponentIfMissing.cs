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
    static class AddPickupableComponentIfMissing
    {
        [HarmonyPatch]
        static class BreakableResource_SpawnResourceFromPrefab_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<BreakableResource>(_ => _.SpawnResourceFromPrefab(default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo GameObject_Instantiate_MI = SymbolExtensions.GetMethodInfo(() => UnityEngine.Object.Instantiate<GameObject>(default(GameObject), default(Vector3), default(Quaternion)));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(GameObject_Instantiate_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.addComponent_MI);
                    }
                }
            }
        }

        static class Hooks
        {
            public static readonly MethodInfo addComponent_MI = SymbolExtensions.GetMethodInfo(() => addComponent(default));
            static void addComponent(GameObject obj)
            {
                obj.EnsureComponent<Pickupable>();
            }
        }
    }
}
