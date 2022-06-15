using GRandomizer.Util;
using GRandomizer.Util.Lifepod;
using HarmonyLib;
using System.Reflection;
using UnityModdingUtility.Extensions;

namespace GRandomizer.MiscPatches
{
    [HarmonyPatch]
    static class Vehicle_EnterVehicle_Patch
    {
        static MethodInfo TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Vehicle), nameof(Vehicle.EnterVehicle));
        }

        static void Prefix(Vehicle __instance)
        {
            if (__instance.HasComponent<VehicleLifepod>() && Inventory.main.Exists())
            {
                Inventory.main.SecureItems(false);
            }
        }
    }
}
