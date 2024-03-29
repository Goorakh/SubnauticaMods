﻿using GRandomizer.RandomizerControllers;
using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Lifepod;
using HarmonyLib;
using QModManager.API.ModLoading;
using SMLHelper.V2.Handlers;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GRandomizer
{
    [QModCore]
    public static class Mod
    {
        internal static RandomizerConfig Config { get; private set; }

        internal static DirectoryInfo ModFolder;

        [QModPatch]
        public static void Patch()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            const string GAME_VER =
#if SN1
                                    "SUBNAUTICA";
#elif BZ
                                    "BELOW ZERO";
#else
#error Invalid game build
#endif

            const string VERBOSE_STR =
#if VERBOSE
                                       "VERBOSE ";
#else
                                       "";
#endif

            const string MOD_VER =
#if DEBUG
                                   "DEBUG";
#else
                                   "RELEASE";
#endif

            const string BUILD_DESCRIPTION = VERBOSE_STR + GAME_VER + " " + MOD_VER + " BUILD";
            Utils.LogInfo(string.Format("Initializing GRandomizer Version {0} (" + BUILD_DESCRIPTION + ")...", assembly.GetName().Version));

            Config = OptionsPanelHandler.Main.RegisterModOptions<RandomizerConfig>();
            ModFolder = new FileInfo(assembly.Location).Directory;

            GlobalObject.CreateIfMissing();

            RandomizerControllerCallbacks.Initialize();

            string modName = ($"gorakh_{assembly.GetName().Name}");
            Utils.LogInfo($"Patching {modName}");
            Harmony harmony = new Harmony(modName);
            harmony.PatchAll(assembly);
            Utils.LogInfo($"{harmony.GetPatchedMethods().Count()} methods patched successfully!");

            LifepodModelInfo.InitializeModelInfoByTypeDictionary();

            TypeCollection.Clear();
        }
    }
}
