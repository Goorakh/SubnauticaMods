using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.MiscPatches
{
    static class EnsurePickupableHasCollider_Patch
    {
        static MethodInfo TargetMethod()
        {
            return AccessTools.DeclaredMethod(typeof(Pickupable), nameof(Pickupable.Awake));
        }

        static void Prefix(Pickupable __instance)
        {
            if (!__instance.HasComponentInChildren<Collider>())
            {
                BoxCollider boxCollider = __instance.gameObject.AddComponent<BoxCollider>();

                if (__instance.gameObject.TryGetModelBounds(out Bounds modelBounds))
                {
                    boxCollider.size = modelBounds.size;
                    boxCollider.center = modelBounds.center;
                }
                else
                {
                    Utils.LogWarning($"Pickupable {__instance} ({__instance.GetTechType()}) has no collider, and no model bounds could be calculated");

                    boxCollider.size = Vector3.one;
                    boxCollider.center = Vector3.zero;
                }
            }
        }
    }
}
