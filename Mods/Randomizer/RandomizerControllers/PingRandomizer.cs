using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.RandomizerControllers
{
    static class PingRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.PingRandomizer;
        }

        static readonly InitializeOnAccess<Dictionary<PingType, PingType>> _pingTypeReplacements = new InitializeOnAccess<Dictionary<PingType, PingType>>(() =>
        {
            return ((PingType[])Enum.GetValues(typeof(PingType))).ToRandomizedReplacementDictionary();
        });

        [HarmonyPatch]
        static class uGUI_Pings_OnAdd_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uGUI_Pings>(_ => _.OnAdd(default, default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions.HookField(AccessTools.DeclaredField(typeof(PingInstance), nameof(PingInstance.pingType)), Hooks.Ldfld_pingType_MI, HookFieldFlags.Ldfld);
            }

            static class Hooks
            {
                public static readonly MethodInfo Ldfld_pingType_MI = SymbolExtensions.GetMethodInfo(() => Ldfld_pingType(default));
                static PingType Ldfld_pingType(PingType original)
                {
                    return IsEnabled() && _pingTypeReplacements.Get.TryGetValue(original, out PingType replacement) ? replacement : original;
                }
            }
        }
    }
}
