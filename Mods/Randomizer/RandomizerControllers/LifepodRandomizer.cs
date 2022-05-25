using GRandomizer.Util;
using GRandomizer.Util.Lifepod;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
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

            static void Postfix()
            {
                if (IsEnabled())
                {
                    _overrideModel.Get.OnLifepodPositioned();
                }
            }
        }

        [HarmonyPatch]
        static class uGUI_SceneIntro_Stop_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uGUI_SceneIntro>(_ => _.Stop(default));
            }

            static void Prefix(bool isInterrupted)
            {
                if (IsEnabled())
                {
                    _overrideModel.Get.EndIntro(isInterrupted);
                }
            }
        }

        [HarmonyPatch]
        static class IntroVignette_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<IntroVignette>(_ => _.Start()));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo IntroVignette_ShouldPlayIntro_MI = SymbolExtensions.GetMethodInfo<IntroVignette>(_ => _.ShouldPlayIntro());

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(IntroVignette_ShouldPlayIntro_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // Dup return value
                        yield return new CodeInstruction(OpCodes.Call, Hooks.ShouldPlayIntro_Postfix_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo ShouldPlayIntro_Postfix_MI = SymbolExtensions.GetMethodInfo(() => ShouldPlayIntro_Postfix(default));
                static void ShouldPlayIntro_Postfix(bool __result)
                {
                    if (IsEnabled() && !__result)
                    {
                        _overrideModel.Get.EndIntro(true);
                    }
                }
            }
        }

        [HarmonyPatch]
        static class uGUI_SceneIntro_ControlsHints_Patch
        {
            static MethodInfo TargetMethod()
            {
                return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<uGUI_SceneIntro>(_ => _.ControlsHints()));
            }

            static bool Prefix(ref bool __result)
            {
                if (IsEnabled() && _overrideModel.Get.DisableTutorial)
                {
                    __result = false;
                    return false;
                }

                return true;
            }
        }
    }
}
