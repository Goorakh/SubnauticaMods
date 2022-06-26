using System.Diagnostics;
using System.Reflection;

namespace PrawnWallWalker
{
    public static class Utils
    {
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
            string methodTag = callerMethod != null ? $"{callerMethod.DeclaringType.Name}.{callerMethod.Name}" : "null";

            QModManager.Utility.Logger.Log(level, $"[{methodTag}]: {log}", null, showOnScreen);
        }
    }
}
