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
            string methodTag = callerMethod?.Name ?? "null";

            QModManager.Utility.Logger.Log(level, $"[{methodTag}]: {log}", null, showOnScreen);
        }

        public static class Random
        {
            public static bool Boolean(float chance = 0.5f)
            {
                return UnityEngine.Random.value <= chance;
            }

            public static Color Color(float a = 1f)
            {
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, a);
            }

            public static Quaternion Rotation => Quaternion.Euler(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));

            public static T EnumValue<T>() where T : Enum
            {
                return (T)Enum.GetValues(typeof(T)).GetRandomOrDefault();
            }

            public unsafe static T EnumFlag<T>() where T : unmanaged
            {
                Type enumType = typeof(T);
                if (!enumType.IsEnum)
                    throw new ArgumentException($"{nameof(T)} is not an enum type");

                if (enumType.GetCustomAttribute(typeof(FlagsAttribute)) == null)
                    throw new ArgumentException($"{nameof(T)} is not a flags enum type");

                long value = 0;
                for (int i = 0; i < sizeof(T); i++)
                {
                    value |= (Boolean() ? 1L : 0L) << i;
                }

                return *(T*)(&value + (sizeof(long) - sizeof(T)));
            }
        }
    }
}
