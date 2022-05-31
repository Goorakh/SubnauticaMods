using GRandomizer.Util;
using HarmonyLib;
using System.Reflection;

namespace GRandomizer.MiscPatches
{
    [HarmonyPatch]
    static class CrashHome_Spawn_Patch
    {
        static MethodInfo TargetMethod()
        {
            return SymbolExtensions.GetMethodInfo<CrashHome>(_ => _.Spawn());
        }

        static bool Prefix(CrashHome __instance)
        {
            if (__instance.HasComponent<DisableSpawn>())
            {
                __instance.spawnTime = -1f;
                return false;
            }

            return true;
        }
    }
}
