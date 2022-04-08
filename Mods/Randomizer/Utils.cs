using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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

        public static void LogError(string log)
        {
            logLevel(QModManager.Utility.Logger.Level.Error, log);
        }

        public static void LogWarning(string log)
        {
            logLevel(QModManager.Utility.Logger.Level.Warn, log);
        }

        public static void DebugLog(string log)
        {
#if DEBUG
            logLevel(QModManager.Utility.Logger.Level.Debug, log);
#endif
        }
        
        static void logLevel(QModManager.Utility.Logger.Level level, string log)
        {
            QModManager.Utility.Logger.Log(level, $"[GR]: {log}");
        }

        public static class Random
        {
            public static Color Color(float a = 1f)
            {
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, a);
            }

            public static Vector3 insidePositiveUnitSphere
            {
                get
                {
                    return Abs(UnityEngine.Random.insideUnitSphere);
                }
            }
        }
    }
}
