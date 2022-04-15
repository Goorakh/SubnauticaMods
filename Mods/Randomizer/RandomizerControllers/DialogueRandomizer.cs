using FMOD;
using FMODUnity;
using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

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
        static Dictionary<string, SpeechSequence> _sequences;

        static bool _isInitialized = false;
        public static void Initialize()
        {
            _sequences = new Dictionary<string, SpeechSequence>();

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

                    if (_sequences.ContainsKey(soundID))
                    {
                        Utils.LogError($"Duplicate sound event path in VOdata.txt: {soundID}", true);
                    }
                    else
                    {
                        _sequences.Add(soundID, new SpeechSequence(currentSpeaker, soundID, subtitleID));
                    }
                }
            }

            SoundPatcher.AddMutator(tryGetReplacementLine);
            SoundPatcher.OnSoundPlayed += onSoundPlayed;

            _isInitialized = true;
        }

        static Dictionary<string, string> _lineReplacements;

        static string tryGetReplacementLine(string original)
        {
            if (!_isInitialized || mode == RandomDialogueMode.Off) // Deliberately including the events that activate during the loading screen, since they might get cached, it could be the only chance to replace them
                return original;

            if (_lineReplacements == null)
            {
                string[] excludeSpeakersStr = ConfigReader.ReadFromFile<string[]>("Configs/DialogueRandomizer::DontRandomizeSpeakers");
                SpeakerType[] excludeSpeakers = new SpeakerType[excludeSpeakersStr.Length];
                for (int i = 0; i < excludeSpeakers.Length; i++)
                {
                    if (!Enum.TryParse(excludeSpeakersStr[i], true, out excludeSpeakers[i]))
                    {
                        Utils.LogError($"Error parsing Configs/DialogueRandomizer.json: Unknown SpeakerType {excludeSpeakersStr[i]}", true);
                    }
                }

                string[] excludeLines = ConfigReader.ReadFromFile<string[]>("Configs/DialogueRandomizer::DontRandomizeLines");

                switch (mode)
                {
                    case RandomDialogueMode.SameSpeaker:
                        _lineReplacements = new Dictionary<string, string>();

                        Dictionary<SpeakerType, List<string>> speakerLines = new Dictionary<SpeakerType, List<string>>();
                        foreach (SpeechSequence sequence in _sequences.Values)
                        {
                            if (excludeSpeakers.Contains(sequence.Speaker) || excludeLines.Contains(value: sequence.SoundEventPath))
                                continue;

                            if (speakerLines.TryGetValue(sequence.Speaker, out List<string> lines))
                            {
                                lines.Add(sequence.SoundEventPath);
                            }
                            else
                            {
                                speakerLines.Add(sequence.Speaker, new List<string> { sequence.SoundEventPath });
                            }
                        }

                        foreach (SpeechSequence sequence in _sequences.Values)
                        {
                            if (excludeSpeakers.Contains(sequence.Speaker) || excludeLines.Contains(value: sequence.SoundEventPath))
                                continue;

                            _lineReplacements.Add(sequence.SoundEventPath, speakerLines[sequence.Speaker].GetAndRemoveRandom());
                        }
                        break;
                    case RandomDialogueMode.Random:
                        _lineReplacements = new Dictionary<string, string>();

                        List<string> sequenceKeys = (from sequence in _sequences.Values
                                                     where !excludeSpeakers.Contains(sequence.Speaker)
                                                     where !excludeLines.Contains(value: sequence.SoundEventPath)
                                                     select sequence.SoundEventPath).ToList();

                        foreach (SpeechSequence sequence in _sequences.Values)
                        {
                            if (excludeSpeakers.Contains(sequence.Speaker) || excludeLines.Contains(value: sequence.SoundEventPath))
                                continue;

                            _lineReplacements.Add(sequence.SoundEventPath, sequenceKeys.GetAndRemoveRandom());
                        }
                        break;
                    default:
                        throw new NotImplementedException($"{mode} is not implemented");
                }
            }

            if (_lineReplacements.TryGetValue(original, out string replacement))
                return replacement;

            return original;
        }

        static void onSoundPlayed(string playedPath)
        {
            if (!_isInitialized || mode == RandomDialogueMode.Off || uGUI.isLoading) // For some reason certain audio events are triggered during the loading screen, ignore these.
                return;

            if (_lineReplacements.ContainsValue(playedPath))
            {
                if (_sequences.TryGetValue(playedPath, out SpeechSequence playedSequence) && playedSequence.HasSubtitles)
                {
                    Subtitles_Add_Patch.IsCorrectedSubtitle = true;
                    Subtitles.main.Add(playedSequence.SubtitleKey);
                    Subtitles_Add_Patch.IsCorrectedSubtitle = false;
                }

#if DEBUG
                Utils.DebugLog($"onSoundPlayed {_lineReplacements.Single(kvp => kvp.Value == playedPath).Key} -> {playedPath}", false);
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
                if (_isInitialized && mode > RandomDialogueMode.Off && _lineReplacements.Keys.Any(line => _sequences[line].SubtitleKey == key))
                {
#if DEBUG
                    Utils.DebugLog($"Subtitles_Add_Patch.Prefix key: {key}, IsCorrectedSubtitle: {IsCorrectedSubtitle}", false);
#endif
                    return IsCorrectedSubtitle;
                }
                else
                {
                    return true;
                }
            }
        }
    }
}
