using HarmonyLib;
using QModManager.API.ModLoading;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PrawnWallWalker
{
    [QModCore]
    public class Main
    {
        [QModPatch]
        public static void Patch()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string modName = ($"gorakh_{assembly.GetName().Name}");
            Utils.LogInfo($"Patching {modName}");
            Harmony harmony = new Harmony(modName);
            harmony.PatchAll(assembly);
            Utils.LogInfo($"{harmony.GetPatchedMethods().Count()} methods patched successfully!");
        }
    }
}
