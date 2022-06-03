using GRandomizer.Util;
using GRandomizer.Util.Lifepod;
using HarmonyLib;
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
        static readonly InitializeOnAccess<LifepodModelInfo> _overrideModel = new InitializeOnAccess<LifepodModelInfo>(() => LifepodModelInfo.GetByType(LifepodModelType.NeptuneRocket));
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

                LifepodModelInfo model = _overrideModel.Get;
                if (model.Type != LifepodModelType.Default)
                {
                    model.Replace(__instance);
                }
            }
        }

        [HarmonyPatch]
        static class EscapePod_StartAtPosition_Patch
        {
            public static bool IsSettingPosition = false;

            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.StartAtPosition(default));
            }

            static void Prefix(ref Vector3 position)
            {
                IsSettingPosition = true;

                if (IsEnabled())
                {
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default)
                    {
                        position = model.GetOverrideLifepodPosition(position);
                    }
                }
            }

            static void Postfix()
            {
                if (IsEnabled())
                {
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default)
                    {
                        model.OnLifepodPositioned();
                    }
                }

                IsSettingPosition = false;
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
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default)
                    {
                        model.EndIntro(isInterrupted);
                    }
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
                        LifepodModelInfo model = _overrideModel.Get;
                        if (model.Type != LifepodModelType.Default)
                        {
                            model.EndIntro(true);
                        }
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
                if (IsEnabled())
                {
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default && model.DisableTutorial)
                    {
                        __result = false;
                        return false;
                    }
                }

                return true;
            }

            static void Postfix(bool __result)
            {
                if (!__result && IsEnabled())
                {
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default)
                    {
                        model.TutorialFinished();
                    }
                }
            }
        }

        [HarmonyPatch]
        static class EscapePod_main_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(AvoidEscapePod), nameof(AvoidEscapePod.Evaluate));
                yield return AccessTools.DeclaredMethod(typeof(AvoidEscapePod), nameof(AvoidEscapePod.StopPerform));
                yield return AccessTools.DeclaredMethod(typeof(AvoidEscapePod), nameof(AvoidEscapePod.Perform));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                FieldInfo EscapePod_main_FI = AccessTools.DeclaredField(typeof(EscapePod), nameof(EscapePod.main));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.LoadsField(EscapePod_main_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, Hooks.GetEscapePod_main_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetEscapePod_main_MI = SymbolExtensions.GetMethodInfo(() => GetEscapePod_main(default));
                static MonoBehaviour GetEscapePod_main(EscapePod main)
                {
                    if (IsEnabled() && VehicleLifepod.Instance.Exists())
                    {
                        return VehicleLifepod.Instance;
                    }

                    return main;
                }
            }
        }

        [HarmonyPatch]
        static class EscapePod_RespawnPlayer_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.RespawnPlayer());
            }

            static bool Prefix()
            {
                if (IsEnabled() && !EscapePod_StartAtPosition_Patch.IsSettingPosition)
                {
                    LifepodModelInfo model = _overrideModel.Get;
                    if (model.Type != LifepodModelType.Default)
                    {
                        model.RespawnPlayer(Player.main);
                        return false;
                    }
                }

                return true;
            }
        }

        [HarmonyPatch]
        static class IntroLifepodDirector_ToggleActiveObjects_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<IntroLifepodDirector>(_ => _.ToggleActiveObjects(default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator generator)
            {
                LocalGenerator localGen = new LocalGenerator(generator);

                int objectsStateParameterIndex = original.FindArgumentIndex(typeof(bool), "on");

                MethodInfo GameObject_SetActive_MI = SymbolExtensions.GetMethodInfo<GameObject>(_ => _.SetActive(default));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(GameObject_SetActive_MI))
                    {
                        LocalBuilder on_tempLocal = localGen.GetLocal(typeof(bool), false);
                        yield return new CodeInstruction(OpCodes.Stloc, on_tempLocal);

                        yield return new CodeInstruction(OpCodes.Dup); // Dup instance (GameObject)
                        yield return new CodeInstruction(OpCodes.Ldloc, on_tempLocal);
                        localGen.ReleaseLocal(on_tempLocal);

                        yield return new CodeInstruction(OpCodes.Call, Hooks.Ldarg_on_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Ldarg_on_MI = SymbolExtensions.GetMethodInfo(() => Ldarg_on(default, default));
                static bool Ldarg_on(GameObject obj, bool on)
                {
                    if (IsEnabled())
                    {
                        if (EscapePod.main.Exists())
                        {
                            LifepodModelInfo model = _overrideModel.Get;
                            if (model != null && model.TryGetIntroLifepodDirectorActiveObjectOverrideState(obj.transform.GetRelativePath(EscapePod.main.transform), out bool overrideState))
                            {
                                return overrideState;
                            }
                        }
                    }

                    return on;
                }
            }
        }
    }
}
