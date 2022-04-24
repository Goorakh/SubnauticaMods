using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GRandomizer
{
    public static class Utils
    {
        public static int GetStringFormatCount(string str)
        {
            return Regex.Matches(str, @"(?<!\{)(?>\{\{)*\{\d(.*?)").Count;
        }

        public static Vector3 Abs(Vector3 vector3)
        {
            vector3.x = Mathf.Abs(vector3.x);
            vector3.y = Mathf.Abs(vector3.y);
            vector3.z = Mathf.Abs(vector3.z);
            return vector3;
        }

        public static IEnumerable<TechType> GetAllDefinedTechTypes()
        {
            foreach (TechType techType in (TechType[])Enum.GetValues(typeof(TechType)))
            {
                yield return techType;
            }
        }

        public static void LogError(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Error, log, showOnScreen);
        }

        public static void LogInfo(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Info, log, showOnScreen);
        }

        public static void LogWarning(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Warn, log, showOnScreen);
        }

#if DEBUG
        public static void DebugLog(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Debug, log, showOnScreen);
        }
#endif

        static void logLevel(QModManager.Utility.Logger.Level level, string log, bool showOnScreen)
        {
            QModManager.Utility.Logger.Log(level, $"[GRandomizer]: {log}", null, showOnScreen);
        }

        public static class Random
        {
            public static Color Color(float a = 1f)
            {
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, a);
            }

            public static Quaternion Rotation => Quaternion.Euler(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));
        }
    }
}
