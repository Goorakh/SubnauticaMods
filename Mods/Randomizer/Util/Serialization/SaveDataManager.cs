using GRandomizer.RandomizerControllers;
using GRandomizer.RandomizerControllers.Callbacks;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util.Serialization
{
    public static class SaveDataManager
    {
        public static SaveDataContainer? Container { get; private set; }

        static string saveFilePath => Path.Combine(SaveLoadManager.GetTemporarySavePath(), "GRandomizer.bin");

        static readonly MethodInfo save_MI = SymbolExtensions.GetMethodInfo(() => save());
        static void save()
        {
            if (!Container.HasValue)
            {
                Utils.LogWarning("No save data container active");
                return;
            }

            using (FileStream fs = new FileStream(saveFilePath, FileMode.Create))
            {
                using (BinaryWriter writer = new BinaryWriter(fs))
                {
                    Container.Value.Serialize(writer);
                }
            }

#if VERBOSE
            Utils.DebugLog($"Save data container serialized to {saveFilePath}");
#endif
        }

        static void load()
        {
            RandomizerControllerCallbacks.Reset();

            string path = saveFilePath;
            if (File.Exists(path))
            {
#if VERBOSE
                Utils.DebugLog($"Reading save data container from {path}");
#endif

                using (FileStream fs = new FileStream(path, FileMode.Open))
                {
                    using (BinaryReader reader = new BinaryReader(fs))
                    {
                        Container = new SaveDataContainer(reader);
                    }
                }

#if VERBOSE
                Utils.DebugLog($"Successfully read save data container from {path}");
#endif
            }
            else
            {
#if VERBOSE
                Utils.DebugLog($"Save data file at {path} does not exist, using default values");
#endif

                Container = new SaveDataContainer(null);
            }
        }

        // Patches generously "donated" by SML :>
        [HarmonyPatch]
        static class IngameMenu_SaveGameAsync_Patch
        {
            static MethodInfo TargetMethod()
            {
                return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<IngameMenu>(_ => _.SaveGameAsync()));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo IngameMenu_CaptureSaveScreenshot_MI = SymbolExtensions.GetMethodInfo<IngameMenu>(_ => _.CaptureSaveScreenshot());

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(IngameMenu_CaptureSaveScreenshot_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, save_MI);
                    }
                }
            }
        }

        [HarmonyPatch]
        static class uGUI_SceneLoading_BeginAsyncSceneLoad_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uGUI_SceneLoading>(_ => _.BeginAsyncSceneLoad(default));
            }

            static void Postfix(string sceneName)
            {
                if (sceneName == "Main")
                {
                    load();
                }
            }
        }
    }
}
