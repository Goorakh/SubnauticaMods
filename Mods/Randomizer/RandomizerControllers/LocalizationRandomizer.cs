using GRandomizer.Util;
using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace GRandomizer.RandomizerControllers
{
    static class LocalizationRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomLocalization;
        }

        static Dictionary<string, string> _randomizedLocalizationDict = null;

        [HarmonyPatch(typeof(Language), nameof(Language.LoadLanguageFile))]
        static class LoadLanguageFile_Patch
        {
            static void Postfix(bool __result, Language __instance)
            {
                if (__result && __instance.Exists() && __instance.strings != null && __instance.strings.Count > 0)
                {
                    Dictionary<int, List<string>> formatCountToStrings = new Dictionary<int, List<string>>();
                    foreach (string localizedString in __instance.strings.Values)
                    {
                        int formatCount = Utils.GetStringFormatCount(localizedString);

                        if (formatCountToStrings.TryGetValue(formatCount, out List<string> formattedStrings))
                        {
                            formattedStrings.Add(localizedString);
                        }
                        else
                        {
                            formatCountToStrings[formatCount] = new List<string>() { localizedString };
                        }
                    }

                    Dictionary<string, string> newLocalizationDictionary = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, string> item in __instance.strings)
                    {
                        int formatCount = Utils.GetStringFormatCount(item.Value);

                        newLocalizationDictionary.Add(item.Key, formatCountToStrings[formatCount].GetAndRemoveRandom());
                    }

                    _randomizedLocalizationDict = newLocalizationDictionary;
                }
                else
                {
                    _randomizedLocalizationDict = null;
                }
            }
        }

        [HarmonyPatch(typeof(Language), nameof(Language.TryGet))]
        static class TryGet_Patch
        {
            static readonly FieldInfo Language_strings_FI = AccessTools.Field(typeof(Language), nameof(Language.strings));

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.LoadsField(Language_strings_FI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, SymbolExtensions.GetMethodInfo(() => Hooks.LoadStringsDict_Hook(default)));
                    }
                }
            }

            static class Hooks
            {
                public static Dictionary<string, string> LoadStringsDict_Hook(Dictionary<string, string> strings)
                {
                    if (IsEnabled())
                    {
                        return _randomizedLocalizationDict ?? strings;
                    }
                    else
                    {
                        return strings;
                    }
                }
            }
        }

        public static class SetCurrentLanguage_Patch
        {
            [HarmonyReversePatch(HarmonyReversePatchType.Snapshot)]
            [HarmonyPatch(typeof(Language), nameof(Language.SetCurrentLanguage))]
            public static void InvokeOnLanguageChanged()
            {
                IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
                {
                    FieldInfo Language_main_FI = AccessTools.Field(typeof(Language), nameof(Language.main));
                    MethodInfo Language_LoadLanguageFile_MI = SymbolExtensions.GetMethodInfo<Language>(_ => _.LoadLanguageFile(default));

                    foreach (CodeInstruction instruction in instructions)
                    {
                        if (instruction.opcode == OpCodes.Ldarg_0)
                        {
                            yield return new CodeInstruction(OpCodes.Ldsfld, Language_main_FI).WithLabels(instruction.labels);
                        }
                        else if (instruction.Calls(Language_LoadLanguageFile_MI))
                        {
                            yield return new CodeInstruction(OpCodes.Pop); // Pop instance
                            yield return new CodeInstruction(OpCodes.Pop); // Pop argument
                            yield return new CodeInstruction(OpCodes.Ldc_I4_0); // Push "false" on stack as method return value
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                }

                Transpiler(null);
            }
        }
    }
}
