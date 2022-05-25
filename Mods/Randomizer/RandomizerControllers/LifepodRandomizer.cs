using GRandomizer.Util;
using GRandomizer.Util.Lifepod;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class LifepodRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.LifepodRandomizer;
        }

#if DEBUG
        static readonly InitializeOnAccess<LifepodModelInfo> _overrideModel = new InitializeOnAccess<LifepodModelInfo>(() => LifepodModelInfo.GetByType(LifepodModelType.Seamoth));
#else
        static readonly InitializeOnAccess<LifepodModelInfo> _overrideModel = new InitializeOnAccess<LifepodModelInfo>(() => LifepodModelInfo.GetByType(Utils.Random.EnumValue<LifepodModelType>()));
#endif

        [HarmonyPatch]
        static class EscapePod_Awake_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.Awake());
            }

            static void Prefix(EscapePod __instance)
            {
                if (!IsEnabled())
                    return;

                _overrideModel.Get.Replace(__instance);
            }
        }

        [HarmonyPatch]
        static class EscapePod_StartAtPosition_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.StartAtPosition(default));
            }

            static void Prefix(Vector3 position)
            {
                if (IsEnabled())
                {
                    _overrideModel.Get.SetPosition(position);
                }
            }
        }
    }
}
