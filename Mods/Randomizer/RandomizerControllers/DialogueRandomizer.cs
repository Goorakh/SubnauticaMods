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

namespace GRandomizer.RandomizerControllers
{
    static class DialogueRandomizer
    {
        static RandomDialogueMode mode => Mod.Config.RandomDialogue;

        struct SoundEntry
        {
            public string SoundID;
            public string Subtitles;

            public SoundEntry(string soundID, string subtitles)
            {
                SoundID = soundID;
                Subtitles = subtitles;
            }

            public SoundEntry(SoundQueue.Entry entry) : this(entry.sound, entry.subtitles)
            {
            }

            public static implicit operator SoundQueue.Entry(SoundEntry entry)
            {
                return new SoundQueue.Entry(entry.SoundID, entry.Subtitles);
            }
            public static implicit operator SoundEntry(SoundQueue.Entry entry)
            {
                return new SoundEntry(entry);
            }
        }

        static readonly Dictionary<string, SoundEntry> _soundCache;
        static readonly Dictionary<SpeakerType, string[]> _speakerEntries;

        static readonly Dictionary<string, string> _lineReplacements = new Dictionary<string, string>();

        static DialogueRandomizer()
        {
            _soundCache = new Dictionary<string, SoundEntry>();
            _speakerEntries = new Dictionary<SpeakerType, string[]>();

            string[] lines = Properties.Resources.VOdata.Split('\n');

            HashSet<string> currentLines = new HashSet<string>();
            SpeakerType currentType = SpeakerType.None;
            for (int i = 0; i < lines.Length; i++)
            {
                const string SPEAKER_PREFIX = "SPEAKER=";
                if (lines[i].StartsWith(SPEAKER_PREFIX))
                {
                    if (currentType != SpeakerType.None)
                    {
                        _speakerEntries.Add(currentType, currentLines.ToArray());
                        currentLines.Clear();
                    }

                    string speakerTypeString = lines[i].Substring(SPEAKER_PREFIX.Length).Trim();
                    currentType = (SpeakerType)Enum.Parse(typeof(SpeakerType), speakerTypeString);
                }
                else if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    string id = lines[i].Trim();
                    if (!currentLines.Add(id))
                    {
                        Utils.LogWarning($"Duplicate line data for {id}", true);
                    }
                    else
                    {
                        _soundCache.Add(id, new SoundEntry(id, string.Empty));
                    }
                }
            }

            _speakerEntries.Add(currentType, currentLines.ToArray());
        }

        static string tryGetReplacementLine(string ID)
        {
            if (_soundCache.TryGetValue(ID, out SoundEntry entry))
            {
                return tryGetReplacementLine(entry).SoundID;
            }
            else
            {
                return ID;
            }
        }
        static SoundEntry tryGetReplacementLine(SoundEntry entry)
        {
            if (mode == RandomDialogueMode.Off)
                return entry;

            if (_lineReplacements.TryGetValue(entry.SoundID, out string value))
            {
                return _soundCache[value];
            }
            else
            {
                KeyValuePair<string, SoundEntry> newSound = _soundCache.ToList().GetRandom();
                _lineReplacements[entry.SoundID] = newSound.Key;

#if DEBUG
                Utils.DebugLog($"Replace sequence {entry.SoundID} -> {newSound.Key}", true);
#endif

                return newSound.Value;
            }
        }

        [HarmonyPatch(typeof(FMOD.Studio.System), nameof(FMOD.Studio.System.getEventByID))]
        static class FMOD_Studio_System_getEventByID_Patch
        {
            static void Prefix(FMOD.Studio.System __instance, ref Guid guid)
            {
                if (mode > RandomDialogueMode.Off)
                {
                    if (__instance.lookupPath(guid, out string path) == RESULT.OK)
                    {
                        string replacementPath = tryGetReplacementLine(path);
                        if (replacementPath != path && __instance.lookupID(replacementPath, out Guid replacementGuid) == RESULT.OK)
                        {
                            guid = replacementGuid;
                        }
                    }
                }
            }
        }
    }
}
