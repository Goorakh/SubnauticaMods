using GRandomizer.MiscPatches;
using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityModdingUtility;
using UnityModdingUtility.Extensions;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class DialogueRandomizer
    {
        static RandomDialogueMode mode => Mod.Config.RandomDialogue;

        static bool IsEnabled()
        {
            return mode > RandomDialogueMode.Off;
        }

        static void Reset()
        {
            _lineReplacements.Reset();
        }

        public static void Serialize(BinaryWriter writer)
        {
            if (writer.WriteAndReturn(_lineReplacements.IsInitialized))
            {
                writer.Write(_lineReplacements.Get);
            }
        }

        public static void Deserialize(VersionedBinaryReader reader)
        {
            if (reader.ReadBoolean()) // _lineReplacements.IsInitialized
            {
                _lineReplacements.SetValue(reader.ReadReplacementDictionary<string>());
            }
        }

        struct SpeechSequence
        {
            public string SpeakerID;
            public string SoundEventPath;
            public string SubtitleKey;

            public bool HasSubtitles => !string.IsNullOrEmpty(SubtitleKey);

            public SpeechSequence(string speaker, string soundEventPath, string subtitleKey)
            {
                SpeakerID = speaker;
                SoundEventPath = soundEventPath;
                SubtitleKey = subtitleKey;
            }

            public override string ToString()
            {
                return $"({nameof(SpeakerID)}: {SpeakerID}, {nameof(SoundEventPath)}: {SoundEventPath}, {nameof(SubtitleKey)}: {SubtitleKey ?? "null"})";
            }
        }
        static readonly InitializeOnAccess<Dictionary<string, SpeechSequence>> _sequences = new InitializeOnAccess<Dictionary<string, SpeechSequence>>(() =>
        {
            Dictionary<string, SpeechSequence> sequences = new Dictionary<string, SpeechSequence>();

            JObject obj = ConfigReader.ReadFromFile<JObject>("Configs/DialogueRandomizer::Lines");
            if (obj == null)
            {
                Utils.LogError("Invalid config! Configs/DialogueRandomizer.json has no 'Lines' property");
                return new Dictionary<string, SpeechSequence>();
            }

            foreach (KeyValuePair<string, JToken> speakerHeader in obj)
            {
                foreach (JToken lineDataToken in (JArray)speakerHeader.Value)
                {
                    string soundID = (string)lineDataToken["SoundID"];
                    string subtitleID = (string)lineDataToken["SubtitleID"];

                    if (sequences.ContainsKey(soundID))
                    {
                        Utils.LogError($"Duplicate sound event path in DialogueRandomizer.json: {soundID}", true);
                    }
                    else
                    {
                        if (subtitleID == null && SubtitlePatcher.CustomSubtitleDataBySoundID.TryGetValue(soundID, out SubtitlePatcher.SubtitleData data))
                            subtitleID = data.LocalizationKey;

                        SpeechSequence sequence = new SpeechSequence(speakerHeader.Key, soundID, subtitleID);
#if VERBOSE
                        Utils.DebugLog($"Loading sequence: {sequence}");
#endif
                        sequences.Add(soundID, sequence);
                    }
                }
            }

            return sequences;
        });

        static readonly InitializeOnAccess<ReplacementDictionary<string>> _lineReplacements = new InitializeOnAccess<ReplacementDictionary<string>>(() =>
        {
            HashSet<string> excludeSpeakers = ConfigReader.ReadFromFile<HashSet<string>>("Configs/DialogueRandomizer::DontRandomizeSpeakers");
            HashSet<string> excludeLines = ConfigReader.ReadFromFile<HashSet<string>>("Configs/DialogueRandomizer::DontRandomizeLines");

            IEnumerable<SpeechSequence> filteredSequences = from sequence in _sequences.Get.Values
                                                            where !excludeSpeakers.Contains(sequence.SpeakerID)
                                                            where !excludeLines.Contains(sequence.SoundEventPath)
                                                            select sequence;

            switch (mode)
            {
                case RandomDialogueMode.SameSpeaker:
                    return new ReplacementDictionary<string>((from sequence in filteredSequences
                                                              group sequence.SoundEventPath by sequence.SpeakerID into gr
                                                              from replacementPair in gr.ToRandomizedReplacementDictionary()
                                                              select replacementPair).ToDictionary());
                case RandomDialogueMode.Random:
                    return filteredSequences.Select(s => s.SoundEventPath).ToRandomizedReplacementDictionary();
                default:
                    throw new NotImplementedException($"RandomDialogueMode.{mode} is not implemented");
            }
        });

        static bool _isInitialized = false;
        static void Initialize()
        {
            SoundPatcher.AddMutator(tryGetReplacementLine);
            SoundPatcher.OnSoundPlayed += onSoundPlayed;

            _isInitialized = true;
        }

        static string tryGetReplacementLine(string original)
        {
            if (!_isInitialized || !IsEnabled()) // Deliberately including the events that activate during the loading screen, since they might get cached, it could be the only chance to replace them
                return original;

            if (_lineReplacements.Get.TryGetReplacement(original, out string replacement))
                return replacement;

            return original;
        }

        static void onSoundPlayed(string playedPath)
        {
            if (!_isInitialized || !IsEnabled() || uGUI.isLoading) // For some reason certain audio events are triggered during the loading screen, ignore these.
                return;

            if (_lineReplacements.Get.TryGetOriginal(playedPath, out string orignial))
            {
                if (_sequences.Get.TryGetValue(playedPath, out SpeechSequence playedSequence) && playedSequence.HasSubtitles)
                {
                    ShowCorrectedSubtitle(playedSequence.SubtitleKey);
                }

#if VERBOSE
                Utils.DebugLog($"{orignial} -> {playedPath}");
#endif
            }
        }

        public static bool TryGetSubtitleKey(string soundID, out string subtitleKey)
        {
            subtitleKey = null;
            return _sequences.Get.TryGetValue(soundID, out SpeechSequence sequence) && (subtitleKey = sequence.SubtitleKey) != null;
        }

        public static void ShowCorrectedSubtitle(string key)
        {
            Subtitles_Add_Patch.IsCorrectedSubtitle = true;
            Subtitles.main.Add(key);
            Subtitles_Add_Patch.IsCorrectedSubtitle = false;
        }

        [HarmonyPatch]
        static class Subtitles_Add_Patch
        {
            // TODO: This patch isn't great, rewrite it at some point

            public static bool IsCorrectedSubtitle;

            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<Subtitles>(_ => _.Add(default));
                yield return SymbolExtensions.GetMethodInfo<Subtitles>(_ => _.Add(default, default));
            }

            static bool Prefix(string key)
            {
                if (_isInitialized && IsEnabled() && _lineReplacements.Get.Any(kvp => _sequences.Get[kvp.Key].SubtitleKey == key))
                {
#if VERBOSE
                    Utils.DebugLog($"key: {key}, IsCorrectedSubtitle: {IsCorrectedSubtitle}");
#endif
                    return IsCorrectedSubtitle;
                }
                else
                {
                    return true;
                }
            }
        }

        [HarmonyPatch(typeof(uGUI_LogEntry), nameof(uGUI_LogEntry.Initialize))]
        static class uGUI_LogEntry_Initialize_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                return instructions.HookField(AccessTools.Field(typeof(PDALog.EntryData), nameof(PDALog.EntryData.key)), Hooks.EntryData_getkey_Hook_MI, HookFieldFlags.Ldfld | HookFieldFlags.IncludeInstance);
            }

            static class Hooks
            {
                public static readonly MethodInfo EntryData_getkey_Hook_MI = SymbolExtensions.GetMethodInfo(() => EntryData_getkey_Hook(default, default));
                static string EntryData_getkey_Hook(PDALog.EntryData entryData, string key)
                {
                    if (_isInitialized && IsEnabled() && entryData != null && entryData.sound.Exists())
                    {
                        if (_lineReplacements.Get.TryGetReplacement(entryData.sound.path, out string replacementPath) && _sequences.Get.TryGetValue(replacementPath, out SpeechSequence sequence))
                        {
                            return sequence.SubtitleKey;
                        }
                    }

                    return key;
                }
            }
        }

        [HarmonyPatch]
        static class uGUI_EncyclopediaTab_DisplayEntry_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uGUI_EncyclopediaTab>(_ => _.DisplayEntry(default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator generator)
            {
                LocalGenerator localGen = new LocalGenerator(generator);

                MethodInfo uGUI_EncyclopediaTab_SetText_MI = SymbolExtensions.GetMethodInfo<uGUI_EncyclopediaTab>(_ => _.SetText(default));

                int entryDataLocalIndex = -1;

                foreach (CodeInstruction instruction in instructions)
                {
                    if (entryDataLocalIndex == -1 && instruction.opcode.IsAny(OpCodes.Ldloca, OpCodes.Ldloca_S))
                    {
                        int localIndex = instruction.GetLocalIndex();

                        if (original.GetMethodBody().LocalVariables[localIndex].LocalType == typeof(PDAEncyclopedia.EntryData))
                            entryDataLocalIndex = localIndex;
                    }

                    if (instruction.Calls(uGUI_EncyclopediaTab_SetText_MI))
                    {
                        if (entryDataLocalIndex == -1)
                        {
                            Utils.LogWarning($"Could not find {nameof(entryDataLocalIndex)} before {nameof(uGUI_EncyclopediaTab)}.{nameof(uGUI_EncyclopediaTab.SetText)} call, skipping");
                        }
                        else
                        {
                            ParameterInfo[] parameters = uGUI_EncyclopediaTab_SetText_MI.GetParameters();
                            LocalBuilder[] locals = new LocalBuilder[parameters.Length];

                            for (int i = parameters.Length - 1; i >= 0; i--)
                            {
                                if (parameters[i].ParameterType == typeof(string))
                                {
                                    yield return new CodeInstruction(OpCodes.Ldloc, entryDataLocalIndex);
                                    yield return new CodeInstruction(OpCodes.Call, Hooks.GetDescriptionText_Hook_MI);

                                    for (int j = i + 1; j < parameters.Length; j++)
                                    {
                                        yield return new CodeInstruction(OpCodes.Ldloc, locals[j]);
                                        localGen.ReleaseLocal(locals[j]);
                                    }

                                    break;
                                }

                                yield return new CodeInstruction(OpCodes.Stloc, locals[i] = localGen.GetLocal(parameters[i].ParameterType, false));
                            }
                        }
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo GetDescriptionText_Hook_MI = SymbolExtensions.GetMethodInfo(() => GetDescriptionText_Hook(default, default));
                static string GetDescriptionText_Hook(string original, PDAEncyclopedia.EntryData entry)
                {
                    if (_isInitialized && IsEnabled() && entry != null && entry.audio.Exists())
                    {
                        if (_lineReplacements.Get.TryGetReplacement(entry.audio.path, out string replacementPath) && _sequences.Get.TryGetValue(replacementPath, out SpeechSequence sequence))
                        {
                            if (sequence.HasSubtitles)
                            {
                                return Language.main.Get(sequence.SubtitleKey);
                            }
                            else
                            {
                                Utils.LogWarning($"{sequence.SoundEventPath} has no subtitle");
                                return "NULL";
                            }
                        }
                    }

                    return original;
                }
            }
        }
    }
}
