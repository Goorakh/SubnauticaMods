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
        public delegate void SoundPlayedDelegate(string path);

        static List<Mutator<string>> _eventPathMutators;
        public static event SoundPlayedDelegate OnSoundPlayed;

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
                if (__instance.lookupPath(guid, out string originalPath) == RESULT.OK)
                {
                    string replacementPath = mutatePath(originalPath);
                    if (replacementPath != originalPath && __instance.lookupID(replacementPath, out Guid replacementGuid) == RESULT.OK)
                    {
                        guid = replacementGuid;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(FMOD.Studio.EventInstance), nameof(FMOD.Studio.EventInstance.start))]
        static class EventInstance_start_Patch
        {
            static void Prefix(FMOD.Studio.EventInstance __instance)
            {
                RESULT result;
                if ((result = __instance.getDescription(out FMOD.Studio.EventDescription description)) == RESULT.OK)
                {
                    if ((result = description.getPath(out string path)) == RESULT.OK)
                    {
                        OnSoundPlayed?.Invoke(path);
                    }
                    else
                    {
                        Utils.LogWarning($"EventInstance.start() Prefix: description.getPath returned {result}", true);
                    }
                }
                else
                {
                    Utils.LogWarning($"EventInstance.start() Prefix: __instance.getDescription returned {result}", true);
                }
            }
        }
    }
}
