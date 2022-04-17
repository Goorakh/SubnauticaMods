using GRandomizer.Util;
using HarmonyLib;
using QModManager.Utility;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using uSky;

namespace GRandomizer.RandomizerControllers
{
    static class ColorRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomColors;
        }

        class ColorReplacer : MonoBehaviour
        {
            static Color selectColorReplacement(Color key)
            {
                return Utils.Random.Color(key.a);
            }

            static readonly InitializeOnAccessDictionary<Color, Color> _globalReplacements = new InitializeOnAccessDictionary<Color, Color>(selectColorReplacement);
            readonly InitializeOnAccessDictionary<Color, Color> _specificReplacements = new InitializeOnAccessDictionary<Color, Color>(selectColorReplacement);

            MonoBehaviour _component;
            bool _isInitialized;

            public void Initialize(MonoBehaviour component)
            {
                _component = component;
                _isInitialized = true;
            }

            void Update()
            {
                if (_isInitialized && (!_component || _component == null))
                {
                    Destroy(this);
                }
            }

            static ColorReplacer getOrAddComponent(MonoBehaviour component)
            {
                ColorReplacer colorReplacer = component.GetComponent<ColorReplacer>();
                if (!colorReplacer || colorReplacer == null)
                {
                    colorReplacer = component.gameObject.AddComponent<ColorReplacer>();
                    colorReplacer.Initialize(component);
                }

                return colorReplacer;
            }

            public Color GetReplacement(Color original)
            {
                return _specificReplacements[original];
            }

            public static Color GetReplacement(Color original, MonoBehaviour component)
            {
                return component == null ? GetGlobalReplacement(original) : getOrAddComponent(component).GetReplacement(original);
            }

            public static Color GetGlobalReplacement(Color original)
            {
                return _globalReplacements[original];
            }
        }

        [HarmonyPatch]
        static class CreateGradient_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo(() => UWE.Utils.ConstantGradient(default));
                yield return SymbolExtensions.GetMethodInfo(() => UWE.Utils.DayNightGradient(default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.IsLdarg(0))
                    {
                        yield return new CodeInstruction(OpCodes.Call, Hooks.ldarg0_Hook_MI);
                    }
                }
            }

            static class Hooks
            {
                public static MethodInfo ldarg0_Hook_MI = SymbolExtensions.GetMethodInfo(() => ldarg0_Hook(default));
                static Color ldarg0_Hook(Color original)
                {
                    if (IsEnabled())
                    {
                        return ColorReplacer.GetGlobalReplacement(original);
                    }
                    else
                    {
                        return original;
                    }
                }
            }
        }

        [HarmonyPatch]
        static class LoadColor_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<BloomCreature>(_ => _.Update());
                yield return SymbolExtensions.GetMethodInfo<Charger>(_ => _.UpdateVisuals(default, default));
                yield return SymbolExtensions.GetMethodInfo<CyclopsHUDSonarPing>(_ => _.LateUpdate());
                yield return SymbolExtensions.GetMethodInfo<CyclopsHUDSonarPing>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<CyclopsSmokeScreenFX>(_ => _.InitMaterial());
                yield return SymbolExtensions.GetMethodInfo<DayNightLight>(_ => _.Evaluate(default));

                long _long;
                yield return SymbolExtensions.GetMethodInfo<ExploderObject>(_ => _.ProcessCutter(out _long));
                yield return SymbolExtensions.GetMethodInfo<ExplosionScreenFX>(_ => _.OnRenderImage(default, default));
                yield return SymbolExtensions.GetMethodInfo<GenericConsole>(_ => _.UpdateState());
                yield return SymbolExtensions.GetMethodInfo<GenericSign>(_ => _.UpdateCanvas());
                yield return SymbolExtensions.GetMethodInfo<GUITextShadow>(_ => _.LateUpdate());
                yield return SymbolExtensions.GetMethodInfo<MapRoomCameraScreenFX>(_ => _.OnRenderImage(default, default));
                yield return SymbolExtensions.GetMethodInfo<mset.Logo>(_ => _.OnGUI());
                yield return SymbolExtensions.GetMethodInfo<NightVision>(_ => _.Start());

                yield return AccessTools.Method(typeof(uGUI_BlueprintEntry), "INotificationTarget.Progress");
                yield return SymbolExtensions.GetMethodInfo<uGUI_CraftNode>(_ => _.UpdateIcon(default));
                yield return SymbolExtensions.GetMethodInfo(() => uGUI_EncyclopediaTab.UpdateNotificationsCount(default, default));
                yield return AccessTools.Method(typeof(uGUI_EquipmentSlot), "INotificationTarget.Progress");
                yield return SymbolExtensions.GetMethodInfo<uGUI_ItemIcon>(_ => _.SetNotificationProgress(default));
                yield return SymbolExtensions.GetMethodInfo<uGUI_ItemIcon>(_ => _.UpdateColor());
                yield return SymbolExtensions.GetMethodInfo<uGUI_ListEntry>(_ => _.Progress(default));
                yield return SymbolExtensions.GetMethodInfo<uGUI_Toolbar>(_ => _.SetNotificationsAmount(default, default));

                yield return SymbolExtensions.GetMethodInfo<OVRBoundary>(_ => _.SetLookAndFeel(default));
                yield return SymbolExtensions.GetMethodInfo<OVRPlatformMenu>(_ => _.Awake());
                yield return AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<OVRScreenFade>(_ => _.FadeIn()));

                yield return SymbolExtensions.GetMethodInfo<RadiationsScreenFX>(_ => _.OnRenderImage(default, default));
                yield return SymbolExtensions.GetMethodInfo<ScannerTool>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<SonarVision>(_ => _.Awake());
                yield return SymbolExtensions.GetMethodInfo<SubFloodAlarm>(_ => _.NewAlarmState());

                yield return SymbolExtensions.GetMethodInfo<AtmosphereDirector>(_ => _.Start());

                yield return SymbolExtensions.GetMethodInfo<SwitchColorChange>(_ => _.Initialize());
                yield return SymbolExtensions.GetMethodInfo<SwitchColorChange>(_ => _.SwapSwitchColor());

                yield return AccessTools.Method(typeof(uGUI_GalleryTab), "Awake");

                yield return SymbolExtensions.GetMethodInfo<uGUI_LogEntry>(_ => _.Initialize(default));
                yield return AccessTools.Method(typeof(uGUI_LogEntry), "INotificationTarget.Progress");

                yield return SymbolExtensions.GetMethodInfo<uGUI_TimeCapsuleTab>(_ => _.UpdateStatus());

                yield return SymbolExtensions.GetMethodInfo<uSky.StarField>(_ => _.InitializeStarfield());
                yield return SymbolExtensions.GetMethodInfo<uSky.uSkyLight>(_ => _.SunAndMoonLightUpdate());

                yield return SymbolExtensions.GetMethodInfo<uSkyManager>(_ => _.SetVaryingMaterialProperties(default));
                yield return SymbolExtensions.GetMethodInfo<uSkyManager>(_ => _.SetConstantMaterialProperties(default));
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "getMoonInnerCorona");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "getMoonOuterCorona");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "bottomTint");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "getNightHorizonColor");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "getPlanetInnerCorona");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "getPlanetOuterCorona");
                yield return AccessTools.PropertyGetter(typeof(uSkyManager), "variableRangeWavelengths");

                yield return SymbolExtensions.GetMethodInfo<UWE.BiomePlot>(_ => _.UpdateBiomePlotInternals());
                yield return SymbolExtensions.GetMethodInfo<UWE.FrameTimeOverlay>(_ => _.OnPostRender());

                yield return SymbolExtensions.GetMethodInfo<VFXAnimator>(_ => _.UpdateColor(default));
                yield return SymbolExtensions.GetMethodInfo<VFXLerpColor>(_ => _.ManagedUpdate());
                yield return SymbolExtensions.GetMethodInfo<VFXPrecursorGunElevator>(_ => _.UpdateWallLights());
                yield return SymbolExtensions.GetMethodInfo<VFXVolumetricLight>(_ => _.UpdateMaterial(default));
                yield return SymbolExtensions.GetMethodInfo<VFXWeatherManager>(_ => _.UpdateClouds());
                yield return SymbolExtensions.GetMethodInfo<VFXWeatherManager>(_ => _.UpdateRain());
                yield return SymbolExtensions.GetMethodInfo<VFXWeatherManager>(_ => _.UpdateSnow());

                yield return SymbolExtensions.GetMethodInfo<SeaMoth>(_ => _.onLightsToggled(default));

                yield return AccessTools.Method(typeof(SimpleFogBoxVolume), "UpdateVolume");

                yield return SymbolExtensions.GetMethodInfo<WaterscapeVolume.Settings>(_ => _.GetEmissive());
                yield return SymbolExtensions.GetMethodInfo<WaterSurface>(_ => _.SetupSurfaceMaterialConstantValues(default));

                yield return SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.MessageStreamingError());
                yield return SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.MessageFeedbackSent());
                yield return SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.Submit(default));

                yield return AccessTools.Method(typeof(uGUI_PopupNotification), "Awake");

                yield return SymbolExtensions.GetMethodInfo<uGUI_TextGradient>(_ => _.ModifyMesh(default(VertexHelper)));

                yield return SymbolExtensions.GetMethodInfo<VoxelandChunk>(_ => _.BuildGrass(default(VoxelandChunk.GrassCB)));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.opcode == OpCodes.Ldfld || instruction.opcode == OpCodes.Ldsfld)
                    {
                        FieldInfo field = instruction.operand as FieldInfo;
                        if (field != null && (field.FieldType == typeof(Color) || field.FieldType == typeof(Color32)))
                        {
                            if (instruction.opcode == OpCodes.Ldsfld || original.IsStatic)
                            {
                                yield return new CodeInstruction(OpCodes.Ldnull);
                            }
                            else
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                            }

                            MethodInfo method;
                            if (field.FieldType == typeof(Color))
                            {
                                method = Hooks.getColor_Hook_MI;
                            }
                            else// if (field.FieldType == typeof(Color32))
                            {
                                method = Hooks.getColor32_Hook_MI;
                            }

                            yield return new CodeInstruction(OpCodes.Call, method);
                        }
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo getColor_Hook_MI = SymbolExtensions.GetMethodInfo(() => getColor_Hook(default, default));
                static Color getColor_Hook(Color original, MonoBehaviour __instance)
                {
                    if (!IsEnabled())
                        return original;

                    return ColorReplacer.GetReplacement(original, __instance);
                }

                public static readonly MethodInfo getColor32_Hook_MI = SymbolExtensions.GetMethodInfo(() => getColor32_Hook(default, default));
                static Color32 getColor32_Hook(Color32 original, MonoBehaviour __instance)
                {
                    return getColor_Hook(original, __instance);
                }
            }
        }

        static void tryRandomizeGradient(Gradient gradient, MonoBehaviour instance)
        {
            if (IsEnabled())
            {
                GradientColorKey[] colorKeys = gradient.colorKeys;
                for (int i = 0; i < colorKeys.Length; i++)
                {
                    colorKeys[i].color = ColorReplacer.GetReplacement(colorKeys[i].color, instance);
                }

                gradient.colorKeys = colorKeys;
            }
        }

        [HarmonyPatch(typeof(DayNightCycle), "Awake")]
        static class DayNightCycle_Awake_Patch
        {
            static void Prefix(DayNightCycle __instance)
            {
                tryRandomizeGradient(__instance.atmosphereColor, __instance);
            }
        }

        [HarmonyPatch(typeof(MapRoomCamera), "Start")]
        static class MapRoomCamera_Start_Patch
        {
            static void Prefix(MapRoomCamera __instance)
            {
                tryRandomizeGradient(__instance.gradientInner, __instance);
                tryRandomizeGradient(__instance.gradientOuter, __instance);
            }
        }

        [HarmonyPatch(typeof(Seaglide), "Start")]
        static class Seaglide_Start_Patch
        {
            static void Prefix(Seaglide __instance)
            {
                tryRandomizeGradient(__instance.gradientInner, __instance);
                tryRandomizeGradient(__instance.gradientOuter, __instance);
            }
        }

        [HarmonyPatch(typeof(uFogGradient), "Start")]
        static class uFogGradient_Start_Patch
        {
            static void Prefix(uFogGradient __instance)
            {
                tryRandomizeGradient(__instance.FogColor, __instance);
            }
        }

        [HarmonyPatch(typeof(uGUI_Bar), "Awake")]
        static class uGUI_Bar_Awake_Patch
        {
            static void Prefix(uGUI_Bar __instance)
            {
                tryRandomizeGradient(__instance.colorBar, __instance);
                tryRandomizeGradient(__instance.colorIcon, __instance);
            }
        }

        [HarmonyPatch(typeof(uSkyLight), "OnEnable")]
        static class uSkyLight_OnEnable_Patch
        {
            static void Prefix(uSkyLight __instance)
            {
                tryRandomizeGradient(__instance.LightColor, __instance);
                tryRandomizeGradient(__instance.Ambient.EquatorColor, __instance);
                tryRandomizeGradient(__instance.Ambient.GroundColor, __instance);
                tryRandomizeGradient(__instance.Ambient.SkyColor, __instance);
            }
        }

        [HarmonyPatch(typeof(uSkyManager), "Awake")]
        static class uSkyManager_Awake_Patch
        {
            static void Prefix(uSkyManager __instance)
            {
                tryRandomizeGradient(__instance.meanSkyColor, __instance);
                tryRandomizeGradient(__instance.NightZenithColor, __instance);
                tryRandomizeGradient(__instance.skyFogColor, __instance);
            }
        }

        [HarmonyPatch(typeof(VFXSunbeam), "Awake")]
        static class VFXSunbeam_Awake_Patch
        {
            static void Prefix(VFXSunbeam __instance)
            {
                tryRandomizeGradient(__instance.cloudsColor, __instance);
            }
        }

        [HarmonyPatch(typeof(WaterBiomeManager), "Start")]
        static class WaterBiomeManager_Start_Patch
        {
            static void Prefix(WaterBiomeManager __instance)
            {
                if (IsEnabled())
                {
                    List<GameObject> skyPrefabs = (from biome in __instance.biomeSettings
                                                   where biome != null && biome.skyPrefab != null
                                                   select biome.skyPrefab).ToList();

                    foreach (WaterBiomeManager.BiomeSettings biome in __instance.biomeSettings)
                    {
                        if (biome != null)
                        {
                            if (biome.skyPrefab != null)
                            {
#if DEBUG
                                int skyIndex = UnityEngine.Random.Range(0, skyPrefabs.Count);
                                Utils.DebugLog($"Replace {biome.name} skyPrefab: {biome.skyPrefab.name}->{skyPrefabs[skyIndex].name}", false);
                                biome.skyPrefab = skyPrefabs[skyIndex];
                                skyPrefabs.RemoveAt(skyIndex);
#else
                                biome.skyPrefab = skyPrefabs.GetAndRemoveRandom();
#endif
                            }

#if DEBUG
                            WaterscapeVolume.Settings oldSettings = (WaterscapeVolume.Settings)biome.settings.MemberwiseClone();
#endif
                            Vector3 rotatedAbsorption = Quaternion.Euler(UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f, UnityEngine.Random.value * 360f) * biome.settings.absorption;
                            biome.settings.absorption = Utils.Abs(rotatedAbsorption) * UnityEngine.Random.Range(0.3f, 2f);

                            biome.settings.scattering *= UnityEngine.Random.Range(-2f, 2f);
                            biome.settings.scatteringColor = ColorReplacer.GetReplacement(biome.settings.scatteringColor, __instance);
                            biome.settings.murkiness *= UnityEngine.Random.Range(0f, 2f);
                            biome.settings.emissive = ColorReplacer.GetReplacement(biome.settings.emissive, __instance);
                            biome.settings.emissiveScale *= UnityEngine.Random.Range(-3f, 3f);
                            biome.settings.startDistance *= UnityEngine.Random.Range(0.5f, 3f);
                            biome.settings.sunlightScale *= UnityEngine.Random.Range(-2f, 2f);
                            biome.settings.ambientScale *= UnityEngine.Random.Range(0f, 2f);
                            
                            if (UnityEngine.Random.value < 0.2f)
                                biome.settings.temperature *= UnityEngine.Random.Range(0f, 3f);

#if DEBUG
                            foreach (FieldInfo field in AccessTools.GetDeclaredFields(typeof(WaterscapeVolume.Settings)))
                            {
                                object oldValue = field.GetValue(oldSettings);
                                object newValue = field.GetValue(biome.settings);

                                if (!oldValue.Equals(newValue))
                                {
                                    Utils.DebugLog($"Replace {biome.name} WaterscapeVolume.Settings.{field.Name}: {oldValue}->{newValue}", false);
                                }
                            }
#endif
                        }
                    }
                }
            }
        }
    }
}
