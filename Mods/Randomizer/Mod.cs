using GRandomizer.RandomizerControllers;
using GRandomizer.Util;
using HarmonyLib;
using QModManager.API.ModLoading;
using QModManager.Utility;
using SMLHelper.V2.Handlers;
using System.IO;
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

            Config = OptionsPanelHandler.Main.RegisterModOptions<RandomizerConfig>();
            ModFolder = new FileInfo(assembly.Location).Directory;

            GlobalObject.CreateIfMissing();
            DialogueRandomizer.Initialize();

            string modName = ($"gorakh_{assembly.GetName().Name}");
            Utils.LogInfo($"Patching {modName}");
            Harmony harmony = new Harmony(modName);
            harmony.PatchAll(assembly);
            Utils.LogInfo("Patched successfully!");
        }
    }
}
