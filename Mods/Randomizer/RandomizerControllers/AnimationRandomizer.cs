using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class AnimationRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.AnimationRandomizer;
        }

        static void Initialize()
        {
            _toolNames.StartAsyncOperation();
        }

        static void Reset()
        {
            _toolNameReplacements.Reset();
        }

        public static void Serialize(BinaryWriter writer)
        {
            if (writer.WriteAndReturn(_toolNameReplacements.IsInitialized))
            {
                writer.Write(_toolNameReplacements.Get);
            }
        }

        public static void Deserialize(VersionedBinaryReader reader)
        {
            if (reader.ReadBoolean()) // _toolNameReplacements.IsInitialized
            {
                _toolNameReplacements.SetValue(reader.ReadReplacementDictionary<string>());
            }
        }

        static IEnumerator initializeToolNamesAsync(IOut<HashSet<string>> result)
        {
#if VERBOSE
            System.Diagnostics.Stopwatch stopwatch = new System.Diagnostics.Stopwatch();
            stopwatch.Start();
#endif

            HashSet<string> toolNames = new HashSet<string>();

            foreach (TechType techType in (TechType[])Enum.GetValues(typeof(TechType)))
            {
                GameObject prefab = CraftData.GetPrefabForTechType(techType, false);
                if (prefab.Exists())
                {
                    PlayerTool tool = prefab.GetComponent<PlayerTool>();
                    if (tool.Exists())
                    {
                        string animToolName = tool.animToolName;
#if VERBOSE
                        Utils.DebugLog($"Found PlayerTool {techType}: [hasAnimations: {tool.hasAnimations}, animToolName: {animToolName ?? "null"}]");
#endif
                        if (tool.hasAnimations && !string.IsNullOrEmpty(animToolName))
                        {
                            toolNames.Add(animToolName);
                        }
                    }
                }

                for (int i = 0; i < 3; i++)
                {
                    yield return null;
                }
            }

#if VERBOSE
            stopwatch.Stop();
            Utils.DebugLog($"Finished loading tool names (took {stopwatch.Elapsed.TotalSeconds:F1}s)");
#endif

            result.Set(toolNames);
        }

        static readonly InitializeAsync<HashSet<string>> _toolNames = new InitializeAsync<HashSet<string>>(initializeToolNamesAsync);

        static readonly InitializeOnAccess<ReplacementDictionary<string>> _toolNameReplacements = new InitializeOnAccess<ReplacementDictionary<string>>(() =>
        {
            return _toolNames.Get.ToRandomizedReplacementDictionary(true);
        });

        [HarmonyPatch]
        static class QuickSlots_SetAnimationState_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<QuickSlots>(_ => _.SetAnimationState(default));
            }

            static void Prefix(ref string toolName)
            {
                if (IsEnabled())
                {
                    if (_toolNameReplacements.Get.TryGetReplacement(toolName, out string replacement))
                    {
                        toolName = replacement;
                    }
                }
            }
        }

        [HarmonyPatch]
        static class ArmsController_SetUsingWelder_Patch
        {
            static MethodInfo TargetMethod()
            {
                MethodInfo ArmsController_InstallAnimationRules_MI = SymbolExtensions.GetMethodInfo<ArmsController>(_ => _.InstallAnimationRules());
                foreach (KeyValuePair<OpCode, object> instructionKvp in PatchProcessor.ReadMethodBody(ArmsController_InstallAnimationRules_MI))
                {
                    if (instructionKvp.Key == OpCodes.Ldftn)
                    {
                        MethodInfo method = (MethodInfo)instructionKvp.Value;
                        if (PatchProcessor.ReadMethodBody(method).Any(kvp => kvp.Key == OpCodes.Ldstr && (string)kvp.Value == "holding_welder"))
                        {
                            return method;
                        }
                    }
                }

                throw new Exception("Could not find OnChange method for welder");
            }

            static bool Prefix()
            {
                return !IsEnabled();
            }
        }

        [HarmonyPatch(typeof(ArmsController), nameof(ArmsController.Start))]
        static class ArmsController_Start_Patch
        {
            static void Postfix(ArmsController __instance)
            {
#if VERBOSE && DEBUG
                foreach (var item in __instance.animator.parameters)
                {
                    Utils.DebugLog($"{item.name}: {item.type}");
                }
#endif

                foreach (AnimationClip animation in __instance.animator.runtimeAnimatorController.animationClips)
                {
                    const string USE_TOOL_MESSAGE = "OnToolUseAnim";

                    float animationEventFraction;
                    switch (animation.name)
                    {
                        case "player_view_airbladder_use":
                        case "player_view_terraformer_panel_open_start":
                        case "transfuser_walks_use":
                        case "player_view_welder_transitiontolaser":
                            animationEventFraction = 1f;
                            break;
                        case "player_view_scanner_scanEnter":
                            animationEventFraction = 1f / 2f;
                            break;
                        case "builder_use_enter":
                            animationEventFraction = 1 / 3f;
                            break;
                        default:
                            animationEventFraction = -1f;
                            break;
                    }

                    if (animationEventFraction >= 0f)
                    {
                        animation.AddEvent(new AnimationEvent
                        {
                            m_Time = animation.length * animationEventFraction,
                            m_FunctionName = "OnToolUseAnim",
                            messageOptions = SendMessageOptions.DontRequireReceiver
                        });
                    }

#if VERBOSE && DEBUG
                    Utils.DebugLog($"{animation.name} ({animation.length}): ");
                    foreach (var evnt in animation.events)
                    {
                        Utils.DebugLog($"\t{evnt.time}: {evnt.functionName}({evnt.intParameter} | {evnt.floatParameter} | {evnt.stringParameter ?? "null"} | {evnt.objectReferenceParameter?.name ?? "null"})");
                    }
#endif
                }
            }
        }

        // The seaglide has no "use" animation, so this is probably the only way to patch it properly
        [HarmonyPatch]
        static class GUIHand_OnUpdate_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<GUIHand>(_ => _.OnUpdate());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                LocalGenerator localGen = new LocalGenerator(generator);

                FieldInfo GUIHand_usedToolThisFrame_FI = AccessTools.DeclaredField(typeof(GUIHand), nameof(GUIHand.usedToolThisFrame));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.StoresField(GUIHand_usedToolThisFrame_FI))
                    {
                        LocalBuilder value = localGen.GetLocal(GUIHand_usedToolThisFrame_FI.FieldType, false);
                        LocalBuilder instance = localGen.GetLocal(GUIHand_usedToolThisFrame_FI.DeclaringType, false);

                        yield return new CodeInstruction(OpCodes.Stloc, value);
                        yield return new CodeInstruction(OpCodes.Stloc, instance);

                        yield return new CodeInstruction(OpCodes.Ldloc, instance);
                        yield return new CodeInstruction(OpCodes.Ldloc, value);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.set_usedToolThisFrame_MI);

                        yield return new CodeInstruction(OpCodes.Ldloc, instance);
                        yield return new CodeInstruction(OpCodes.Ldloc, value);

                        localGen.ReleaseLocal(value);
                        localGen.ReleaseLocal(instance);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo set_usedToolThisFrame_MI = SymbolExtensions.GetMethodInfo(() => set_usedToolThisFrame(default, default));
                static void set_usedToolThisFrame(GUIHand __instance, bool usedToolThisFrame)
                {
                    if (IsEnabled() && __instance.Exists())
                    {
                        if (usedToolThisFrame)
                        {
                            PlayerTool activeTool = __instance.GetTool();
                            if (activeTool.Exists())
                            {
                                if (_toolNameReplacements.Get.TryGetReplacement(activeTool.animToolName, out string replacement) && replacement == "seaglide")
                                {
                                    activeTool.OnToolUseAnim(__instance);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
