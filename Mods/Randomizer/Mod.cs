using HarmonyLib;
using QModManager.API.ModLoading;
using QModManager.Utility;
using SMLHelper.V2.Handlers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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

            Config = OptionsPanelHandler.Main.RegisterModOptions<RandomizerConfig>();
            ModFolder = new FileInfo(assembly.Location).Directory;

            string modName = ($"gorakh_{assembly.GetName().Name}");
            Logger.Log(Logger.Level.Info, $"Patching {modName}");
            Harmony harmony = new Harmony(modName);
            harmony.PatchAll(assembly);
            Logger.Log(Logger.Level.Info, "Patched successfully!");
        }
    }
}
