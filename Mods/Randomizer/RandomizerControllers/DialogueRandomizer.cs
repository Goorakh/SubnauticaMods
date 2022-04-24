using GRandomizer.Util;
using HarmonyLib;
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
            public SpeakerType Speaker;
            public string SoundEventPath;
            public string SubtitleKey;

            public bool HasSubtitles => !string.IsNullOrEmpty(SubtitleKey);

            public SpeechSequence(SpeakerType speaker, string soundEventPath, string subtitleKey)
            {
                Speaker = speaker;
                SoundEventPath = soundEventPath;
                SubtitleKey = subtitleKey;
            }
        }
        static readonly InitializeOnAccess<Dictionary<string, SpeechSequence>> _sequences = new InitializeOnAccess<Dictionary<string, SpeechSequence>>(() =>
        {
            Dictionary<string, SpeechSequence> sequences = new Dictionary<string, SpeechSequence>();

            SpeakerType currentSpeaker = SpeakerType.None;
            string[] lines = Properties.Resources.VOdata.Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                const string SPEAKER_PREFIX = "SPEAKER=";
                if (lines[i].StartsWith(SPEAKER_PREFIX))
                {
                    string speakerTypeString = lines[i].Substring(8 /*SPEAKER_PREFIX.Length*/).Trim();
                    currentSpeaker = (SpeakerType)Enum.Parse(typeof(SpeakerType), speakerTypeString);
                }
                else
                {
                    string soundID = lines[i].Trim();
                    string subtitleID = null;

                    int subtitleSeparatorIndex = soundID.IndexOf('|');
                    if (subtitleSeparatorIndex != -1)
                    {
                        subtitleID = soundID.Substring(subtitleSeparatorIndex + 1);
                        soundID = soundID.Remove(subtitleSeparatorIndex);
                    }

                    if (sequences.ContainsKey(soundID))
                    {
                        Utils.LogError($"Duplicate sound event path in VOdata.txt: {soundID}", true);
                    }
                    else
                    {
                        sequences.Add(soundID, new SpeechSequence(currentSpeaker, soundID, subtitleID));
                    }
                }
            }

            return sequences;
        });

        static readonly InitializeOnAccess<Dictionary<string, string>> _lineReplacements = new InitializeOnAccess<Dictionary<string, string>>(() =>
        {
            string[] excludeSpeakersStr = ConfigReader.ReadFromFile<string[]>("Configs/DialogueRandomizer::DontRandomizeSpeakers");
            SpeakerType[] excludeSpeakers = new SpeakerType[excludeSpeakersStr.Length];
            for (int i = 0; i < excludeSpeakers.Length; i++)
            {
                if (!Enum.TryParse(excludeSpeakersStr[i], true, out excludeSpeakers[i]))
                {
                    Utils.LogError($"Error parsing Configs/DialogueRandomizer.json: Unknown SpeakerType {excludeSpeakersStr[i]} in DontRandomizeSpeakers", true);
                }
            }

            string[] excludeLines = ConfigReader.ReadFromFile<string[]>("Configs/DialogueRandomizer::DontRandomizeLines");

            Dictionary<string, SpeechSequence> filteredSequences = new Dictionary<string, SpeechSequence>();
            foreach (KeyValuePair<string, SpeechSequence> kvp in _sequences.Get)
            {
                if (excludeSpeakers.Contains(kvp.Value.Speaker) || excludeLines.Contains(value: kvp.Key))
                    continue;

                filteredSequences.Add(kvp.Key, kvp.Value);
            }

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            switch (mode)
            {
                case RandomDialogueMode.SameSpeaker:
                    Dictionary<SpeakerType, List<string>> speakerLines = new Dictionary<SpeakerType, List<string>>();

                    foreach (SpeechSequence sequence in filteredSequences.Values)
                    {
                        if (speakerLines.TryGetValue(sequence.Speaker, out List<string> lines))
                        {
                            lines.Add(sequence.SoundEventPath);
                        }
                        else
                        {
                            speakerLines.Add(sequence.Speaker, new List<string> { sequence.SoundEventPath });
                        }
                    }

                    foreach (SpeechSequence sequence in filteredSequences.Values)
                    {
                        replacements.Add(sequence.SoundEventPath, speakerLines[sequence.Speaker].GetAndRemoveRandom());
                    }
                    break;
                case RandomDialogueMode.Random:
                    List<string> sequenceKeys = filteredSequences.Keys.ToList();

                    foreach (SpeechSequence sequence in filteredSequences.Values)
                    {
                        replacements.Add(sequence.SoundEventPath, sequenceKeys.GetAndRemoveRandom());
                    }
                    break;
                default:
                    throw new NotImplementedException($"{mode} is not implemented");
            }

            return replacements;
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
                Utils.DebugLog($"onSoundPlayed {_lineReplacements.Get.Single(kvp => kvp.Value == playedPath).Key} -> {playedPath}");
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
                    Utils.DebugLog($"Subtitles_Add_Patch.Prefix key: {key}, IsCorrectedSubtitle: {IsCorrectedSubtitle}");
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
