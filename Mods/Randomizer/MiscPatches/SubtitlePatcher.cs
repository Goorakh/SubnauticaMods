using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace GRandomizer.MiscPatches
{
    static class SubtitlePatcher
    {
        public const string LOCALIZATION_KEY_PREFIX = "GR_SUBTITLE_";

        public struct SubtitleData
        {
            public readonly string SoundID;
            public readonly string LocalizationKey;
            public readonly string Text;

            public SubtitleData(string soundID, string text, int startDelay = 0)
            {
                SoundID = soundID;
                LocalizationKey = LOCALIZATION_KEY_PREFIX + Guid.NewGuid();

                Text = (startDelay != 0 ? Language.delayHintSequence + startDelay : string.Empty) + text;
            }
        }

        public const string PREFIX_BASE = "HABITAT: ";
        public const string PREFIX_CYCLOPS = "Cyclops: ";
        public const string PREFIX_NEPTUNE = "NEPTUNE: ";
        public const string PREFIX_SEAMOTH = "Seamoth: ";

        // Subtitles for lines that normally don't have any
        static readonly SubtitleData[] subtitleData = new SubtitleData[]
        {
            // Base
            new SubtitleData("event:/sub/base/power_10_3D", PREFIX_BASE + "Caution: Base power at 10%"),
            new SubtitleData("event:/sub/base/power_30_3D", PREFIX_BASE + "Caution: Base power at 30%"),
            new SubtitleData("event:/sub/base/hull_increase", PREFIX_BASE + "Hull integrity increased."),
            new SubtitleData("event:/sub/base/hull_warning", PREFIX_BASE + "Warning: Hull breach imminent."),
            new SubtitleData("event:/sub/base/hull_decrease", PREFIX_BASE + "Caution: Hull integrity decreased."),

            // Cyclops
            new SubtitleData("event:/sub/cyclops/AI_shields", PREFIX_CYCLOPS + "Activating defensive shields."),
            new SubtitleData("event:/sub/cyclops/AI_emergency_speed", PREFIX_CYCLOPS + "Emergency speed."),
            new SubtitleData("event:/sub/cyclops/AI_power_low", PREFIX_CYCLOPS + "Power: Low."),
            new SubtitleData("event:/sub/cyclops/explode_countdown", PREFIX_CYCLOPS + "Total hull failure in: 5... 4... 3... 2... 1..."),

            // Rocket
            new SubtitleData("event:/sub/rocket/telemerty_on", $"{PREFIX_NEPTUNE}Telemetric systems online. Exit vectors calculating...", 2400),
            
            // PDA
            new SubtitleData("event:/loot/new_PDA_data", "Integrating new PDA data.", 2500),
            new SubtitleData("event:/player/Precursor_LostRiverBase_Log_4", "Signal for: Alien thermal plant, added."),
            new SubtitleData("event:/player/goal_lifepod1", "Situational assessment. Time: T+3 hours since planetfall. Lifepod hull: Secure. Communications: Offline."),
            new SubtitleData("event:/player/new_objective_added", "New objectives added."),
            new SubtitleData("event:/player/story/Precursor_Gun_DataDownload2", "Analyzing data broadcast..."),
            new SubtitleData("event:/player/scan_aurora", "Scanning the Aurora: Zero lifesigns detected. Lethal radiation levels detected."),
            new SubtitleData("event:/player/goal_lifepod3", "Environment: Uncharted ocean planet. Oxygen-nitrogen atmosphere. Water contamination: High."),
            new SubtitleData("event:/player/mapped", "Region mapped."),
            new SubtitleData("event:/player/story/Goal_BiomeDeepGrandReef", "Detecting a titanium mass somewhere in this area. Unable to confirm whether it originated on the Aurora."),
            new SubtitleData("event:/player/goal_danger", "Objective is in danger."),
            new SubtitleData("event:/player/change_mission", "Modifying mission parameters."),
            new SubtitleData("event:/player/story/Precursor_Gun_DataDownload1", "Analyzing data broadcast..."),
            new SubtitleData("event:/player/new_creature", "New creature discovered."),
            new SubtitleData("event:/player/open_pda", "Open your PDA to see your current goals."),
            new SubtitleData("event:/player/infection_scan_advise", "Caution: Detecting atypical fluctuations in blood plasma proteins, a self-scan is strongly advised."),
            new SubtitleData("event:/player/oxygen_50", "Oxygen: 50%"),
            new SubtitleData("event:/tools/scanner/new_blueprint", "New blueprint acquired.", 2500),
            new SubtitleData("event:/player/goal_Intro1", "Lifepods are equipped with a fabricator, programmed to construct tools, and render organic substances edible."),
            new SubtitleData("event:/player/scan_planet", "Scanning environment: Planet appears to be submerged, aquatic ecosystem detected.\nAn extremely dangerous energy field exists in the planet's stratosphere."),
            new SubtitleData("event:/player/story/Goal_BiomeCrashZone", "Hazardous radiation detected. Fabricate a radiation suit to protect against undesirable side effects."),
            new SubtitleData("event:/player/oxygen_25", "Oxygen: 25%"),
            new SubtitleData("event:/player/main_brief", "Primary directive: Investigate and eliminate the stratospheric energy field before the colony arrives. If the field remains they will not survive atmospheric entry.\nIn order to disperse the energy threat: explore, study, and modify the planet's biosphere, geology, and atmosphere, catalog new species, secure food and energy, and gather data on unusual phenomenon.\n###1500Mission completion within the allotted timeframes is imperative. The forthcoming colonists must survive."),
            new SubtitleData("event:/player/new_tech", "New technology created."),
            new SubtitleData("event:/player/goal_BiomeKelpForest2", "Short-range scans show a cave system, rich in fossilized remains beneath this area."),
            new SubtitleData("event:/tools/scanner/new_PDA_data", "Integrating new PDA data.", 2500),
            new SubtitleData("event:/player/story/Ending_zinger", "Welcome home to Alterra. Permission to land will be granted once you have settled your outstanding balance of: 1 trillion credits."),
            new SubtitleData("event:/player/story/Goal_BiomeBloodKelp2", "Short-range scans show a cave system, rich in fossilized remains beneath this area."),
            new SubtitleData("event:/player/gun_disabled_pda", "Attention."),
            new SubtitleData("event:/player/oxygen_10", "Warning. Oxygen: 10%"),
            new SubtitleData("event:/player/blood_loss", "Warning: Blood loss detected."),
            new SubtitleData("event:/player/batterly_low", "Battery: Low."),

            // Seamoth
            new SubtitleData("event:/sub/seamoth/torpedo_disarmed", PREFIX_SEAMOTH + "Torpedo systems disengaged."),
            new SubtitleData("event:/sub/seamoth/undock", PREFIX_SEAMOTH + "All systems online.", 5500),
            new SubtitleData("event:/sub/base/enter_seamoth_left", PREFIX_SEAMOTH + "All systems online.", 5500),
            new SubtitleData("event:/sub/base/enter_seamoth_right", PREFIX_SEAMOTH + "All systems online.", 5500),
            new SubtitleData("event:/sub/seamoth/hull_breach_warning", PREFIX_SEAMOTH + "Emergency: Hull breach detected."),
            new SubtitleData("event:/sub/seamoth/torpedo_armed", PREFIX_SEAMOTH + "Torpedoes armed."),
            new SubtitleData("event:/sub/seamoth/hull_fix", PREFIX_SEAMOTH + "Hull integrity restored, draining systems initiated."),

            // Player (lol)
            new SubtitleData("event:/player/jump", "Huugh!"),
            new SubtitleData("event:/player/end_freedive", "Oooohh.. hssss, haaaa, hssss, hoooo"),
            new SubtitleData("event:/player/Pain_no_tank_light", "Uuuungh!"),
            new SubtitleData("event:/player/eat", "*crunch*"),
            new SubtitleData("event:/player/Pain", "Eeeeooooooh... hssss, hooo, hsss, hooo, hsss"),
            new SubtitleData("event:/player/drink_stillsuit", "*gulp (piss)*"),
            new SubtitleData("event:/player/drink", "*gulp*"),
            new SubtitleData("event:/player/Pain_no_tank", "Eeeeooooooooggghhh!"),
            new SubtitleData("event:/player/coughing", "*coughing*"),
            new SubtitleData("event:/player/Pain_surface", "Aooofhh!"),

            // Story placeholder voice
            new SubtitleData("event:/player/sunbeam_destroy", "Receiving emergency message on short range communications. Patching you through.\nSunbeam Captain: Aurora survivors, this is Sunbeam. We've made low orbit and we're ready to break atmosphere and descend on your position, hold tight.\nPDA VO: Warning: Detecting massive energy buildup at sea level.\n*Sound of weapon firing*\nSunbeam Captain: Aurora, our scanners must be malfunctioning-\n*Explosion*", 750),
            new SubtitleData("event:/player/enzyme_cure", "When a player eats concentrated enzymes they're cured! Play normal cure sound in here, along with some inspirational music.\n###3000When a player eats concentrated enzymes they're cured! Play normal cure sound in he-", 1500),
            new SubtitleData("event:/player/aurora_last_transmission", "Aurora T-6 minutes: This is an emergency distress call, Aurora is on collision course with planet 4546B, sending all available environmental data. Please find survival solution.\nALTERRA HQ T+8 hours: To any survivors of the starship Aurora, if you're hearing this, we will do everything in our power to get you home. A rescue mission would take years to reach you out there, but we think we've got a better solution.\nALTERRA HQ T+8 hours: We recieved your environmental data, and we've sent you a blueprint for a specially designed ship we think will be capable of breaking orbit and getting you back to the nearest phasegate. We calculate that you can find the requisite materials in situ. We'll be sure to- *static*\nT+8 hours: Communications relay offline.", 1000),
            new SubtitleData("event:/player/gun_disabled", "Local power systems are shutting down, orbital maneuvers have now been deemed safe.", 1500),
            new SubtitleData("event:/player/gun_door_open", "Authorizations can complete, subject shows no sign of infection. Manual override access granted.", 1000),
            new SubtitleData("event:/player/sick_reveal", "Using acquired bacterial data to test for user infection. Results will be output to the databank. Delay tense. Urgent warning: Infection test returned positive, results have been output to the databank. Immediate attention is required.", 1500),
            new SubtitleData("event:/player/blast_off", "Launching in 10 seconds... Warming engine. Releasing clamps. Engaging flight planning. 3... 2... 1... Liftoff!", 1500),
            new SubtitleData("event:/player/sunbeam_rescue", "This is trading vessel Sunbeam. Aurora, we've recieved your emergency transmission and we're coming in for a closer look. Plan is to engage a slow-burn and slingshot around the moon to conserve fuel.\nWe'll alert you when we drop into low orbit around the planet. Stay safe, Sunbeam out.", 1500),

            // Misc
            new SubtitleData("event:/player/access_granted", "\u2580\u2596\u2517\u259b\u2584\u2596\u259c\u259a\u2523 \u259c\u259a\u2517\u2523\u2517\u252b\u2513\u250f\u2513 \u259b\u2584\u2596\u2505\u2517\u2596. \u2523\u2517\u250f\u259b\u2584\u2596\u259c\u250f\u2523 \u259a \u2596\u259e\u2523\u2517\u2596\u2517\u2523. \u2523\u2517\u2596\u2503\u2580\u259a\u2597\u250f\u250f\u2513."),
            new SubtitleData("event:/player/access_denied", "\u2517\u259b\u2584\u2596\u259c\u259a\u2523 \u259c\u259a\u2517\u2523\u2517\u252b\u2513\u250f\u2513 \u259b\u2584\u2596\u2505\u2517\u2596. \u2523\u2517\u250f\u259b\u2584\u2596\u259c\u250f\u2523. \u2517\u259b\u2584\u2596\u259c\u259a\u2523"),
            new SubtitleData("event:/sub/pod/radio/radioMushroom24", "High priority automated message from Aurora lifepod 13, coordinates attached.\nLifepod contains the last known remains of high priority mongolian passenger. Send immediate burial detail."),
            // Causes a lot of sound events to not work after being played, find a fix for this or exclude it completely
            //new SubtitleData("event:/main_menu/intro", "In the late 22nd century, humanity is beginning to colonize space. Before colony ships arrive, habitation vessels are appointed terraforming missions, the Aurora, was one such vessel.\nDuring it's descent the Aurora was struck by a mysterious energy pulse resulting in catastrophic hull failure.\nA single lifepod jettisoned prior to impact; You were in that lifepod."),
            new SubtitleData("event:/player/story/Deepgrandreef", "\u2580\u2596\u2517\u259b\u2584\u2596\u259c\u259a\u2523 \u259c\u259a\u2517\u2523\u2517\u252b\u2513\u250f\u2513 \u259b\u2584\u2596\u2505\u2517\u2596. \u2523\u2517\u250f\u259b\u2584\u2596\u259c\u250f\u2523"),
            new SubtitleData("event:/sub/base/coffeemachine_idle", "You look like you could use some coffee ^_^"),
            new SubtitleData("event:/sub/base/make_coffee", "*whirring*\n###17000Coffee completed ╰(*°▽°*)╯"),
            new SubtitleData("event:/tools/dolls/mark", "*Markiplier noises*"),
            new SubtitleData("event:/tools/dolls/jack", "*Jacksepticeye noises*"),
            new SubtitleData("event:/sub/pod/radio/radio_generic", "*chatter*"),
            new SubtitleData("event:/sub/pod/radio/radioRadiationSuit", "This is an automated distress signal from lifepod 4, coordinates attached.\nSince planetfall pod has not sustained damage, no crewmembers have disembarked.\nZero lifesigns have been detected onboard, recommend investigation."),
            new SubtitleData("event:/sub/pod/radio/radio_lifepod_17", "This is automated distress signal from Aurora lifepod 17, coordinates attached.\nPlease send immediate emergency relief team."),
            new SubtitleData("event:/player/gunterminal_access_granted", "\u259c\u259a\u2517\u2523\u2517\u252b\u2513\u250f\u2513 \u259b\u2584\u2596\u2505\u2517\u2596. \u2523\u2517\u250f\u259b\u2584\u2596\u259c\u250f\u2523.")
        };

        public static readonly ReadOnlyDictionary<string, SubtitleData> CustomSubtitleDataBySoundID = new ReadOnlyDictionary<string, SubtitleData>(subtitleData.ToDictionary(s => s.SoundID));

        [HarmonyPatch]
        static class Language_LoadLanguageFile_Patch
        {
            static MethodBase TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<Language>(_ => _.LoadLanguageFile(default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                int languageArgIndex = original.FindArgumentIndex(typeof(string));
                MethodInfo Dictionary_string_string_Clear_MI = SymbolExtensions.GetMethodInfo<Dictionary<string, string>>(_ => _.Clear());

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(Dictionary_string_string_Clear_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // Dup string dictionary

                        yield return instruction;

                        //yield return new CodeInstruction(OpCodes.Ldarg, languageArgIndex);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.PatchStringsDictionary_MI);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo PatchStringsDictionary_MI = SymbolExtensions.GetMethodInfo(() => PatchStringsDictionary(default));
                static void PatchStringsDictionary(Dictionary<string, string> strings)
                {
                    foreach (SubtitleData subtitle in subtitleData)
                    {
                        if (!strings.ContainsKey(subtitle.LocalizationKey))
                        {
                            if (subtitle.Text == null)
                            {
                                Utils.LogError($"subtitle.Text for {subtitle.LocalizationKey} is null!", true);
                            }
                            else
                            {
                                strings.Add(subtitle.LocalizationKey, subtitle.Text);
                            }
                        }
                        else
                        {
                            Utils.LogWarning($"Subtitle key {subtitle.LocalizationKey} is already in the dictionary!");
                        }
                    }
                }
            }
        }
    }
}
