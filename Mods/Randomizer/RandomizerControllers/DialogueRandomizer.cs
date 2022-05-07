﻿using GRandomizer.Util;
using HarmonyLib;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GRandomizer.RandomizerControllers
{
    static class DialogueRandomizer
    {
        static RandomDialogueMode mode => Mod.Config.RandomDialogue;

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

        static readonly InitializeOnAccess<Dictionary<string, string>> _lineReplacements = new InitializeOnAccess<Dictionary<string, string>>(() =>
        {
            HashSet<string> excludeSpeakers = ConfigReader.ReadFromFile<HashSet<string>>("Configs/DialogueRandomizer::DontRandomizeSpeakers");
            HashSet<string> excludeLines = ConfigReader.ReadFromFile<HashSet<string>>("Configs/DialogueRandomizer::DontRandomizeLines");

            Dictionary<string, SpeechSequence> filteredSequences = (from sequence in _sequences.Get.Values
                                                                    where !excludeSpeakers.Contains(sequence.SpeakerID)
                                                                    where !excludeLines.Contains(sequence.SoundEventPath)
                                                                    select sequence).ToDictionary(s => s.SoundEventPath);

            switch (mode)
            {
                case RandomDialogueMode.SameSpeaker:
                    return (from sequence in filteredSequences.Values
                            group sequence.SoundEventPath by sequence.SpeakerID into gr
                            from replacementPair in gr.ToRandomizedReplacementDictionary()
                            select replacementPair).ToDictionary();
                case RandomDialogueMode.Random:
                    return filteredSequences.Keys.ToRandomizedReplacementDictionary();
                default:
                    throw new NotImplementedException($"{mode} is not implemented");
            }
        });

        static bool _isInitialized = false;
        public static void Initialize()
        {
            SoundPatcher.AddMutator(tryGetReplacementLine);
            SoundPatcher.OnSoundPlayed += onSoundPlayed;

            _isInitialized = true;
        }

        static string tryGetReplacementLine(string original)
        {
            if (!_isInitialized || mode == RandomDialogueMode.Off) // Deliberately including the events that activate during the loading screen, since they might get cached, it could be the only chance to replace them
                return original;

            if (_lineReplacements.Get.TryGetValue(original, out string replacement))
                return replacement;

            return original;
        }

        static void onSoundPlayed(string playedPath)
        {
            if (!_isInitialized || mode == RandomDialogueMode.Off || uGUI.isLoading) // For some reason certain audio events are triggered during the loading screen, ignore these.
                return;

            if (_lineReplacements.Get.ContainsValue(playedPath))
            {
                if (_sequences.Get.TryGetValue(playedPath, out SpeechSequence playedSequence) && playedSequence.HasSubtitles)
                {
                    Subtitles_Add_Patch.IsCorrectedSubtitle = true;
                    Subtitles.main.Add(playedSequence.SubtitleKey);
                    Subtitles_Add_Patch.IsCorrectedSubtitle = false;
                }

#if VERBOSE
                Utils.DebugLog($"{_lineReplacements.Get.Single(kvp => kvp.Value == playedPath).Key} -> {playedPath}");
#endif
            }
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
                if (_isInitialized && mode > RandomDialogueMode.Off && _lineReplacements.Get.Keys.Any(line => _sequences.Get[line].SubtitleKey == key))
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
                FieldInfo EntryData_key_FI = AccessTools.Field(typeof(PDALog.EntryData), nameof(PDALog.EntryData.key));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.LoadsField(EntryData_key_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // Dup instance

                        yield return instruction;

                        yield return new CodeInstruction(OpCodes.Call, Hooks.EntryData_getkey_Hook_MI);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo EntryData_getkey_Hook_MI = SymbolExtensions.GetMethodInfo(() => EntryData_getkey_Hook(default, default));
                static string EntryData_getkey_Hook(PDALog.EntryData entryData, string key)
                {
                    if (_isInitialized && entryData != null && entryData.sound != null)
                    {
                        if (_lineReplacements.Get.TryGetValue(entryData.sound.path, out string replacementPath) && _sequences.Get.TryGetValue(replacementPath, out SpeechSequence sequence))
                        {
                            return sequence.SubtitleKey;
                        }
                    }

                    return key;
                }
            }
        }
    }
}
