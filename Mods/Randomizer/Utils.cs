using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
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
            return new Vector3(Mathf.Abs(vector3.x), Mathf.Abs(vector3.y), Mathf.Abs(vector3.z));
        }

        public static TechType[] GetAllDefinedTechTypes()
        {
            return (TechType[])Enum.GetValues(typeof(TechType));
        }

        public static void LogError(string log, bool showOnScreen = false, int stackOffset = 0)
        {
            logLevel(QModManager.Utility.Logger.Level.Error, log, showOnScreen, stackOffset);
        }

        public static void LogInfo(string log, bool showOnScreen = false, int stackOffset = 0)
        {
            logLevel(QModManager.Utility.Logger.Level.Info, log, showOnScreen, stackOffset);
        }

        public static void LogWarning(string log, bool showOnScreen = false, int stackOffset = 0)
        {
            logLevel(QModManager.Utility.Logger.Level.Warn, log, showOnScreen, stackOffset);
        }

#if VERBOSE
        public static void DebugLog(string log, bool showOnScreen = false, int stackOffset = 0)
        {
            logLevel(QModManager.Utility.Logger.Level.Debug, log, showOnScreen, stackOffset);
        }
#endif

        static void logLevel(QModManager.Utility.Logger.Level level, string log, bool showOnScreen, int stackOffset)
        {
            MethodBase callerMethod = new StackTrace().GetFrame(2 + stackOffset)?.GetMethod();
            string methodTag = callerMethod != null ? callerMethod.Name : "null";

            QModManager.Utility.Logger.Log(level, $"[{methodTag}]: {log}", null, showOnScreen);
        }

        public static float Clamp01RollOver(float value)
        {
            return (value % 1f) + (value < 0f ? 1f : 0f);
        }

        public static class Random
        {
            public static Color Color(float a = 1f)
            {
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, a);
            }

            public static Quaternion Rotation => Quaternion.Euler(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));

            public static T EnumValue<T>() where T : Enum
            {
                return (T)Enum.GetValues(typeof(T)).GetRandomOrDefault();
            }
        }
    }
}
