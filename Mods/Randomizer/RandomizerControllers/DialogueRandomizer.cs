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

        static readonly string[] _allLines;
        static readonly Dictionary<SpeakerType, string[]> _speakerEntries;
        static readonly Dictionary<string, SpeakerType> _lineToSpeaker;

        static readonly Dictionary<string, string> _lineReplacements = new Dictionary<string, string>();

        // TODO: Make some kind of two-way dictionary type instead of using 2 variables
        static readonly Dictionary<string, string> _lineToSubtitle = new Dictionary<string, string>();
        static readonly Dictionary<string, string[]> _subtitleToLine = new Dictionary<string, string[]>();

        static DialogueRandomizer()
        {
            _speakerEntries = new Dictionary<SpeakerType, string[]>();
            _lineToSpeaker = new Dictionary<string, SpeakerType>();

            string[] lines = Properties.Resources.VOdata.Split('\n');

            HashSet<string> allLines = new HashSet<string>();
            HashSet<string> currentLines = new HashSet<string>();
            SpeakerType currentSpeaker = SpeakerType.None;
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                const string SPEAKER_PREFIX = "SPEAKER=";
                if (lines[i].StartsWith(SPEAKER_PREFIX))
                {
                    if (currentSpeaker != SpeakerType.None)
                    {
                        _speakerEntries.Add(currentSpeaker, currentLines.ToArray());
                        currentLines.Clear();
                    }

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

                        _lineToSubtitle.Add(soundID, subtitleID);

                        if (!_subtitleToLine.TryGetValue(subtitleID, out string[] sounds))
                            sounds = Array.Empty<string>();

                        _subtitleToLine[subtitleID] = sounds.AddToArray(soundID);
                    }

                    if (!currentLines.Add(soundID))
                    {
                        Utils.LogWarning($"Duplicate line data for {soundID}", true);
                    }
                    else
                    {
                        _lineToSpeaker.Add(soundID, currentSpeaker);
                    }

                    allLines.Add(soundID);
                }
            }

            _speakerEntries.Add(currentSpeaker, currentLines.ToArray());
            _allLines = allLines.ToArray();

            SoundPatcher.AddMutator(tryGetReplacementLine);
        }

        static string tryGetReplacementLine(string original)
        {
            if (mode == RandomDialogueMode.Off)
                return original;

            if (_lineReplacements.TryGetValue(original, out string replacement))
            {
                return replacement;
            }
            else if (_lineToSpeaker.TryGetValue(original, out SpeakerType speaker)) // If there isn't a speaker for this event then it is not one we should replace
            {
                string replacementSoundPath;
                switch (mode)
                {
                    case RandomDialogueMode.SameSpeaker:
                        replacementSoundPath = _speakerEntries[speaker].GetRandom();
                        break;
                    case RandomDialogueMode.Random:
                        replacementSoundPath = _allLines.GetRandom();
                        break;
                    default:
                        throw new NotImplementedException($"{mode} is not implemented");
                }

#if DEBUG
                Utils.DebugLog($"Replace sequence {original} -> {replacementSoundPath}", true);
#endif

                return _lineReplacements[original] = replacementSoundPath;
            }
            else
            {
                return original;
            }
        }

        [HarmonyPatch]
        static class Subtitles_Add_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<Subtitles>(_ => _.Add(default));
                yield return SymbolExtensions.GetMethodInfo<Subtitles>(_ => _.Add(default, default));
            }

            // TODO: Ensure subtitles shown for lines that normally wouldn't have subtitles
            static bool Prefix(ref string key)
            {
                if (_subtitleToLine != null && _subtitleToLine.TryGetValue(key, out string[] lines))
                {
                    string originalLine;
                    if (lines.Length == 1)
                    {
                        originalLine = lines[0];
                    }
                    else
                    {
                        // TODO: Fix this somehow
                        originalLine = lines.GetRandom();
                        /*
                        int subtitleLineStartFrame = Time.frameCount;

                        // Wait until next update to read which audio event was played
                        GlobalObject.RunNextFrame(() =>
                        {
                            if (SoundPatcher.GetLastPlayedSound(out string originalLine, out int lineStartFrame) && lineStartFrame == subtitleLineStartFrame)
                            {
                                __instance.popup.phase = uGUI_PopupMessage.Phase.Zero;
                            }
                        });
                        */
                    }

                    string replacementLine = tryGetReplacementLine(originalLine);
                    if (_lineToSubtitle.TryGetValue(replacementLine, out string replacementSubtitle))
                    {
                        key = replacementSubtitle;
                    }
                    else
                    {
                        // No subtitle for replacement line -> Don't show subtitle
                        return false;
                    }
                }

                return true;
            }
        }
    }
}
