using FMOD;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    public static class SoundPatcher
    {
        static string _lastSoundPlayed;
        static int _lastSoundPlayFrame;
        public static bool GetLastPlayedSound(out string eventPath, out int startFrame)
        {
            if (_lastSoundPlayed != null)
            {
                eventPath = _lastSoundPlayed;
                startFrame = _lastSoundPlayFrame;
                return true;
            }

            eventPath = default;
            startFrame = default;
            return false;
        }

        static List<Mutator<string>> _eventPathMutators;

        public static void AddMutator(Mutator<string> pathMutator)
        {
            if (_eventPathMutators == null)
                _eventPathMutators = new List<Mutator<string>>();

            _eventPathMutators.Add(pathMutator);
        }

        static string mutatePath(string path)
        {
            return _eventPathMutators != null ? _eventPathMutators.Mutate(path) : path;
        }

        [HarmonyPatch(typeof(FMOD.Studio.System), nameof(FMOD.Studio.System.getEventByID))]
        static class FMOD_Studio_System_getEventByID_Patch
        {
            static void Prefix(FMOD.Studio.System __instance, ref Guid guid)
            {
                if (__instance.lookupPath(guid, out string path) == RESULT.OK)
                {
                    string replacementPath = mutatePath(path);
                    if (replacementPath != path && __instance.lookupID(replacementPath, out Guid replacementGuid) == RESULT.OK)
                    {
                        guid = replacementGuid;
                    }

                    _lastSoundPlayed = replacementPath;
                    _lastSoundPlayFrame = Time.frameCount;
                }
            }
        }

        [HarmonyPatch(typeof(FMOD_CustomEmitter), nameof(FMOD_CustomEmitter.Awake))]
        static class FMOD_CustomEmitter_Awake_Patch
        {
            static void Prefix(FMOD_CustomEmitter __instance)
            {
                __instance.asset.path = mutatePath(__instance.asset.path);
                _lastSoundPlayed = __instance.asset.path;
                _lastSoundPlayFrame = Time.frameCount;
            }
        }
    }
}
