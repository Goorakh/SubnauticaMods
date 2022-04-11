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

            List<string> currentLines = new List<string>();
            SpeakerType currentType = SpeakerType.None;
            for (int i = 0; i < lines.Length; i++)
            {
                const string SPEAKER_PREFIX = "SPEAKER=";
                if (lines[i].StartsWith(SPEAKER_PREFIX))
                {
                    if (currentType != SpeakerType.None)
                    {
                        _speakerEntries.Add(currentType, currentLines.ToArray());
                    }

                    string speakerTypeString = lines[i].Substring(SPEAKER_PREFIX.Length).Trim();
                    currentType = (SpeakerType)Enum.Parse(typeof(SpeakerType), speakerTypeString);
                }

                if (!string.IsNullOrWhiteSpace(lines[i]))
                {
                    string id = lines[i].Trim();
                    currentLines.Add(id);
                    _soundCache.Add(id, new SoundEntry(id, string.Empty));
                }
            }

            _speakerEntries.Add(currentType, currentLines.ToArray());
        }

        static SoundEntry tryGetReplacementLine(SoundEntry entry)
        {
            _soundCache[entry.SoundID] = entry;

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

        [HarmonyPatch(typeof(SoundQueue), nameof(SoundQueue.Update))]
        static class SoundQueue_Update_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo List_SoundQueue_Entry_get_Item_MI = SymbolExtensions.GetMethodInfo<List<SoundQueue.Entry>, SoundQueue.Entry>(_ => _[default]);

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(List_SoundQueue_Entry_get_Item_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, Hooks.Get_Entry_MI);
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Get_Entry_MI = SymbolExtensions.GetMethodInfo(() => Get_Entry(default));
                static SoundQueue.Entry Get_Entry(SoundQueue.Entry original)
                {
                    return tryGetReplacementLine(original);
                }
            }
        }
    }
}
