using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class PingRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.PingRandomizer;
        }

        public static void Serialize(BinaryWriter writer)
        {
            if (writer.WriteAndReturn(_pingTypeReplacements.IsInitialized))
            {
                writer.Write(_pingTypeReplacements.Get);
            }
        }

        public static void Deserialize(VersionedBinaryReader reader)
        {
            if (reader.ReadBoolean()) // _pingTypeReplacements.IsInitialized
            {
                _pingTypeReplacements.SetValue(reader.ReadReplacementDictionary<PingType>());
            }
        }

        static readonly InitializeOnAccess<ReplacementDictionary<PingType>> _pingTypeReplacements = new InitializeOnAccess<ReplacementDictionary<PingType>>(() =>
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
                    return IsEnabled() && _pingTypeReplacements.Get.TryGetReplacement(original, out PingType replacement) ? replacement : original;
                }
            }
        }
    }
}
