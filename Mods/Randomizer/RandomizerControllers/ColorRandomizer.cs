using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UI.Collections;
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
            static readonly Vector3 _globalColorOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
            readonly Vector3 _colorOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));

            Behaviour _component;
            bool _isInitialized;

            public void Initialize(Behaviour component)
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

            static ColorReplacer getOrAddComponent(Behaviour component)
            {
                ColorReplacer colorReplacer = component.GetComponent<ColorReplacer>();
                if (!colorReplacer.Exists())
                {
                    colorReplacer = component.gameObject.AddComponent<ColorReplacer>();
                    colorReplacer.Initialize(component);
                }

                return colorReplacer;
            }

            static Color rotateColor(Color original, Vector3 offset)
            {
                return new Color(Mathf.Abs(Mathf.Sin((original.r + offset.x) * (2f * Mathf.PI))),
                                 Mathf.Abs(Mathf.Sin((original.g + offset.y) * (2f * Mathf.PI))),
                                 Mathf.Abs(Mathf.Sin((original.b + offset.z) * (2f * Mathf.PI))),
                                 original.a);
            }

            public Color GetReplacement(Color original)
            {
                return rotateColor(original, _colorOffset);
            }

            public static Color GetReplacement(Color original, Behaviour component)
            {
                return component.Exists() ? GetGlobalReplacement(original) : getOrAddComponent(component).GetReplacement(original);
            }

            public static void ReplaceColor(ref Color color, Behaviour component)
            {
                color = GetReplacement(color, component);
            }

            public static readonly MethodInfo tryGetReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetReplacement(default, default));
            static Color tryGetReplacement(Color original, Behaviour component)
            {
                return IsEnabled() ? GetReplacement(original, component) : original;
            }

            public static Color GetGlobalReplacement(Color original)
            {
                return rotateColor(original, _globalColorOffset);
            }

            public static void ReplaceColor(ref Color color)
            {
                color = GetGlobalReplacement(color);
            }

            public static readonly MethodInfo tryGetGlobalReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetGlobalReplacement(default));
            static Color tryGetGlobalReplacement(Color original)
            {
                return IsEnabled() ? GetGlobalReplacement(original) : original;
            }
        }

        public static void Initialize()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
        }

        static void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            if (IsEnabled())
            {
                randomizeRenderSettings();
            }
        }

        static void randomizeRenderSettings()
        {
            if (RenderSettings.fog = UnityEngine.Random.value > 0.2f)
            {
                RenderSettings.fogStartDistance = UnityEngine.Random.Range(10f, 100f);
                RenderSettings.fogEndDistance = RenderSettings.fogStartDistance + UnityEngine.Random.Range(10f, 100f);
                RenderSettings.fogMode = Utils.Random.EnumValue<FogMode>();
                RenderSettings.fogColor = Utils.Random.Color(RenderSettings.fogColor.a);
                RenderSettings.fogDensity = UnityEngine.Random.Range(0f, 2f);
            }

            RenderSettings.ambientMode = Utils.Random.EnumValue<AmbientMode>();
            RenderSettings.ambientSkyColor = Utils.Random.Color(RenderSettings.ambientSkyColor.a);
            RenderSettings.ambientEquatorColor = Utils.Random.Color(RenderSettings.ambientEquatorColor.a);
            RenderSettings.ambientGroundColor = Utils.Random.Color(RenderSettings.ambientGroundColor.a);
            RenderSettings.ambientIntensity = UnityEngine.Random.Range(0f, 2f);
            RenderSettings.ambientLight = Utils.Random.Color(RenderSettings.ambientLight.a);

            if (RenderSettings.sun.Exists())
            {
                randomizeLight(RenderSettings.sun);
            }
        }

        static void randomizeLight(Light light)
        {
            light.color = Utils.Random.Color(light.color.a);

            if (light.type == LightType.Spot && UnityEngine.Random.value < 0.3f)
            {
                float mult = UnityEngine.Random.Range(0.5f, 1.5f);

                light.spotAngle = Mathf.Clamp(light.spotAngle * mult, 0f, 180f);
                light.innerSpotAngle = Mathf.Clamp(light.innerSpotAngle * mult, 0f, 180f);
            }
        }

        static void randomizeGradient(Gradient gradient)
        {
            GradientColorKey[] colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = Utils.Random.Color(colorKeys[i].color.a);
            }

            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = UnityEngine.Random.value;
            }

            gradient.SetKeys(colorKeys, alphaKeys);
        }

        [HarmonyPatch(typeof(uFogGradient), nameof(uFogGradient.Start))]
        static class uFogGradient_Start_Patch
        {
            static void Prefix(uFogGradient __instance)
            {
                if (IsEnabled())
                {
                    randomizeGradient(__instance.FogColor);
                }
            }
        }

        [HarmonyPatch(typeof(uSkyManager), nameof(uSkyManager.Awake))]
        static class uSkyManager_Awake_Patch
        {
            static void Postfix(uSkyManager __instance)
            {
                if (IsEnabled())
                {
#if VERBOSE
                    Dictionary<FieldInfo, object> originalFieldValues = (from field in __instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                                      where !field.FieldType.IsClass
                                                                      select new KeyValuePair<FieldInfo, object>(field, field.GetValue(__instance))).ToDictionary();
#endif

                    __instance.SunDirection = UnityEngine.Random.Range(-180f, 180f);
                    __instance.sunMaxAngle = UnityEngine.Random.Range(0f, 90f);

                    __instance.NorthPoleOffset = UnityEngine.Random.Range(-60f, 60f);

                    __instance.Exposure = UnityEngine.Random.Range(0f, 3f);
                    __instance.RayleighScattering = UnityEngine.Random.Range(0f, 5f);
                    __instance.MieScattering = UnityEngine.Random.Range(0f, 5f);

                    __instance.SunAnisotropyFactor = UnityEngine.Random.Range(0f, 1f);
                    __instance.SunSize = UnityEngine.Random.Range(0f, 10f);

                    __instance.Wavelengths = Utils.Abs(Utils.Random.Rotation * __instance.Wavelengths) * UnityEngine.Random.Range(1f / 2f, 2f);

                    __instance.SkyTint = Utils.Random.Color(__instance.SkyTint.a);
                    __instance.m_GroundColor = Utils.Random.Color(__instance.m_GroundColor.a);

                    __instance.skyFogDensity = Mathf.Pow(UnityEngine.Random.value, 5f) / 1000f;
                    randomizeGradient(__instance.skyFogColor);

                    __instance.planetRadius = UnityEngine.Random.Range(0f, 1000f);
                    __instance.planetZenith = UnityEngine.Random.Range(0f, 360f);
                    __instance.planetDistance *= UnityEngine.Random.Range(1f / 3f, 3f);
                    __instance.planetRimColor = Utils.Random.Color(__instance.planetRimColor.a);
                    __instance.planetAmbientLight = Utils.Random.Color(__instance.planetAmbientLight.a);
                    __instance.planetOrbitSpeed *= UnityEngine.Random.Range(1f / 3f, 3f);
                    __instance.planetLightWrap = UnityEngine.Random.Range(0, 1f);
                    __instance.planetInnerCorona = Utils.Random.Color(UnityEngine.Random.value);
                    __instance.planetOuterCorona = Utils.Random.Color(UnityEngine.Random.value);

                    __instance.cloudsRotateSpeed *= UnityEngine.Random.Range(1f / 3f, 3f);
                    __instance.cloudNightBrightness = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 2f);
                    __instance.cloudsAttenuation = UnityEngine.Random.Range(0f, 1f);
                    __instance.cloudsAlphaSaturation = UnityEngine.Random.Range(1f, 10f);

                    __instance.sunColorMultiplier = UnityEngine.Random.Range(0f, 8f);

                    __instance.skyColorMultiplier = UnityEngine.Random.Range(0f, 8f);

                    __instance.cloudsScatteringMultiplier = UnityEngine.Random.Range(0f, 10f);
                    __instance.cloudsScatteringExponent = UnityEngine.Random.Range(0f, 30f);

                    randomizeGradient(__instance.meanSkyColor);

                    __instance.secondaryLightPow = UnityEngine.Random.Range(0f, 10f);

                    randomizeGradient(__instance.NightZenithColor);
                    __instance.NightHorizonColor = Utils.Random.Color(__instance.NightHorizonColor.a);
                    __instance.StarIntensity = UnityEngine.Random.Range(0f, 10f);

                    __instance.MoonInnerCorona = Utils.Random.Color(UnityEngine.Random.value);
                    __instance.MoonOuterCorona = Utils.Random.Color(UnityEngine.Random.value);
                    __instance.MoonSize = UnityEngine.Random.Range(0f, 1f);

                    __instance.directLightFraction = UnityEngine.Random.value;
                    __instance.indirectLightFraction = UnityEngine.Random.value;

                    __instance.endSequenceLightIntensity = UnityEngine.Random.Range(0f, 1.5f);
                    __instance.endSequenceSunSizeMultiplier = UnityEngine.Random.Range(0f, 1.5f);
                    __instance.endSequencePlanetRadius *= UnityEngine.Random.Range(0.7f, 1.3f);

                    __instance.endSequenceLightColor = Utils.Random.Color(__instance.endSequenceLightColor.a);
                    __instance.endSequenceSunBurstColor = Utils.Random.Color(__instance.endSequenceSunBurstColor.a);

#if VERBOSE
                    foreach (KeyValuePair<FieldInfo, object> original in originalFieldValues)
	                {
                        object newValue = original.Key.GetValue(__instance);
                        if (!Equals(original.Value, newValue))
                        {
                            Utils.DebugLog($"uSkyManager.{original.Key.Name}: {original.Value} -> {newValue}");
                        }
	                }
#endif
                }
            }
        }

        [HarmonyPatch(typeof(uSkyAmbient), MethodType.Constructor)]
        static class uSkyAmbient_ctor_Patch
        {
            static void Postfix(uSkyAmbient __instance)
            {
                if (IsEnabled())
                {
                    randomizeGradient(__instance.SkyColor);
                    randomizeGradient(__instance.EquatorColor);
                    randomizeGradient(__instance.GroundColor);
                }
            }
        }

        [HarmonyPatch(typeof(uSkyLight), MethodType.Constructor)]
        static class uSkyLight_ctor_Patch
        {
            static void Postfix(uSkyLight __instance)
            {
                if (IsEnabled())
                {
                    randomizeGradient(__instance.LightColor);
                }
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
#if VERBOSE
                                int skyIndex = UnityEngine.Random.Range(0, skyPrefabs.Count);
                                Utils.DebugLog($"Replace {biome.name} skyPrefab: {biome.skyPrefab.name}->{skyPrefabs[skyIndex].name}");
                                biome.skyPrefab = skyPrefabs.GetAndRemove(skyIndex);
#else
                                biome.skyPrefab = skyPrefabs.GetAndRemoveRandom();
#endif
                            }

#if VERBOSE
                            WaterscapeVolume.Settings oldSettings = (WaterscapeVolume.Settings)biome.settings.MemberwiseClone();
#endif
                            biome.settings.absorption = Utils.Abs(Utils.Random.Rotation * biome.settings.absorption) * UnityEngine.Random.Range(0.3f, 2f);
                            biome.settings.scattering *= UnityEngine.Random.Range(-2f, 2f);
                            biome.settings.scatteringColor = ColorReplacer.GetReplacement(biome.settings.scatteringColor, __instance);
                            biome.settings.murkiness *= UnityEngine.Random.Range(0f, 2f);
                            biome.settings.emissive = ColorReplacer.GetReplacement(biome.settings.emissive, __instance);
                            biome.settings.emissiveScale *= UnityEngine.Random.Range(-2f, 2f);
                            biome.settings.startDistance *= UnityEngine.Random.Range(0.5f, 3f);
                            biome.settings.sunlightScale *= UnityEngine.Random.Range(-2f, 2f);
                            biome.settings.ambientScale *= UnityEngine.Random.Range(0f, 2f);

                            if (UnityEngine.Random.value < 0.2f)
                                biome.settings.temperature *= UnityEngine.Random.Range(0f, 3f);

#if VERBOSE
                            foreach (FieldInfo field in AccessTools.GetDeclaredFields(typeof(WaterscapeVolume.Settings)))
                            {
                                object oldValue = field.GetValue(oldSettings);
                                object newValue = field.GetValue(biome.settings);

                                if (!oldValue.Equals(newValue))
                                {
                                    Utils.DebugLog($"Replace {biome.name} WaterscapeVolume.Settings.{field.Name}: {oldValue}->{newValue}");
                                }
                            }
#endif
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        static class GraphicRegistry_RegisterGraphicForCanvas_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => GraphicRegistry.RegisterGraphicForCanvas(default, default));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                MethodInfo IndexedSet_AddUnique_MI = SymbolExtensions.GetMethodInfo<IndexedSet<Graphic>>(_ => _.AddUnique(default));
                MethodInfo IndexedSet_Add_MI = SymbolExtensions.GetMethodInfo<IndexedSet<Graphic>>(_ => _.Add(default));

                LocalBuilder graphic = generator.DeclareLocal(typeof(Graphic));

                foreach (CodeInstruction instruction in instructions)
                {
                    bool isAddUnique = instruction.Calls(IndexedSet_AddUnique_MI);
                    if (isAddUnique || instruction.Calls(IndexedSet_Add_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Dup); // Dup Graphic instance
                        yield return new CodeInstruction(OpCodes.Stloc, graphic);

                        yield return instruction;

                        yield return new CodeInstruction(isAddUnique ? OpCodes.Dup : OpCodes.Ldc_I4_1); // Dup AddUnique return value or push 'true' onto stack if regular 'Add' was called

                        yield return new CodeInstruction(OpCodes.Ldloc, graphic);
                        yield return new CodeInstruction(OpCodes.Call, Hooks.OnNewGraphicAdded_MI);
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo OnNewGraphicAdded_MI = SymbolExtensions.GetMethodInfo(() => OnNewGraphicAdded(default, default));
                static void OnNewGraphicAdded(bool isNew, Graphic graphic)
                {
                    if (IsEnabled() && isNew)
                    {
                        graphic.color = ColorReplacer.GetReplacement(graphic.color, graphic);
                    }
                }
            }
        }

        [HarmonyPatch]
        static class BloomCreature_Awake_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<BloomCreature>(_ => _.Awake());
            }

            static void Prefix(BloomCreature __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.attractColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class Charger_Awake_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<Charger>(_ => _.Awake());
            }

            static void Prefix(Charger __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.colorEmpty, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.colorHalf, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.colorFull, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class CyclopsHUDSonarPing_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<CyclopsHUDSonarPing>(_ => _.Start());
            }

            static void Prefix(CyclopsHUDSonarPing __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.passiveColor, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.aggressiveColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class CyclopsLightingPanel_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<CyclopsLightingPanel>(_ => _.Start());
            }

            static void Prefix(CyclopsLightingPanel __instance)
            {
                if (IsEnabled())
                {
                    foreach (object obj in __instance.floodlightsHolder.transform)
                    {
                        Light light = ((Transform)obj).GetComponent<Light>();
                        if (light != null)
                        {
                            randomizeLight(light);
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        static class CyclopsSmokeScreenFX_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<CyclopsSmokeScreenFX>(_ => _.Start());
            }

            static void Prefix(CyclopsSmokeScreenFX __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.color, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class DiveReel_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<DiveReel>(_ => _.Start());
            }

            static void Prefix(DiveReel __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.baseColor, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.lowAmmoColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class ExplosionScreenFX_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<ExplosionScreenFX>(_ => _.Start());
            }

            static void Prefix(ExplosionScreenFX __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.color, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class FakeSunShafts_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<FakeSunShafts>(_ => _.Start());
            }

            static void Prefix(FakeSunShafts __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.beamColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class ToggleLights_OnEnable_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<ToggleLights>(_ => _.OnEnable());
            }

            static void Postfix(ToggleLights __instance)
            {
                if (IsEnabled())
                {
                    foreach (Light light in __instance.lightsParent.GetComponentsInChildren<Light>())
                    {
                        randomizeLight(light);
                    }
                }
            }
        }

        [HarmonyPatch]
        static class GenericConsole_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<GenericConsole>(_ => _.Start());
            }

            static void Prefix(GenericConsole __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.colorUnused, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.colorUsed, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class GUITextShadow_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<GUITextShadow>(_ => _.Start());
            }

            static void Prefix(GUITextShadow __instance)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref __instance.shadowColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class Generic_ReplaceColor_Patch
        {
            struct PatchInfo
            {
                [Flags]
                public enum Flags : byte
                {
                    None = 0,
                    Field = 1 << 0, // Replace value of all Ld(s)fld instructions where the field type is Color or Color32
                    Call = 1 << 1, // Replace the return value on all methods returning Color or Color32
                    ConstColorProperty = 1 << 2, // Replace the return value of only the static Color properties like Color.black, Color.red, etc.
                    Constructor = 1 << 3, // Replace the "return value" of any invoked Color or Color32 constructor
                    All = byte.MaxValue
                }

                public MethodBase Target;
                public Flags PatchFlags;

                public PatchInfo(Flags flags, MethodBase target)
                {
                    PatchFlags = flags;
                    Target = target;
                }
            }

            static readonly InitializeOnAccess<Dictionary<MethodBase, PatchInfo>> _patches = new InitializeOnAccess<Dictionary<MethodBase, PatchInfo>>(() =>
            {
                return new PatchInfo[]
                {
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<ActionProgress>(_ => _.DrawDestroyProgressBar())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<AnteChamber>(_ => _.CacheMaterials())),
                    new PatchInfo(PatchInfo.Flags.Field, SymbolExtensions.GetMethodInfo(() => Builder.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<Constructable>(_ => _.ReplaceMaterials(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<ConstructorBuildBot>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<CrafterGhostModel>(_ => _.UpdateModel(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<CyclopsDecoyLaunchButton>(_ => _.UpdateText())),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<CyclopsExternalCams>(_ => _.SetLight())),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<CyclopsHelmHUDManager>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredMethod(typeof(DevConsole), nameof(DevConsole.Awake))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<DiveReel>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<ElevatorCallControl>(_ => _.CycleArrows())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<ElevatorCallControl>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<EndCreditsManager>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo(() => EntitySlot.GetGhostMaterial(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.StopIntroCinematic(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<EscapePod>(_ => _.UpdateDamagedEffects())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<Gravsphere>(_ => _.UpdatePads())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<HUDFPS>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<IntroLifepodDirector>(_ => _.ConcludeIntroSequence())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<IntroLifepodDirector>(_ => _.OnProtoDeserializeObjectTree(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<KeypadDoorConsole>(_ => _.NumberButtonPress(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<KeypadDoorConsole>(_ => _.ResetNumberField())),
                }.ToDictionary(p => p.Target);
            });

            static IEnumerable<MethodBase> TargetMethods()
            {
                return _patches.Get.Keys;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                MethodInfo ColorToColor32_MI = AccessTools.DeclaredMethod(typeof(Color32), "op_Implicit", new Type[] { typeof(Color) });
                MethodInfo Color32ToColor_MI = AccessTools.DeclaredMethod(typeof(Color32), "op_Implicit", new Type[] { typeof(Color32) });

                PatchInfo patchInfo = _patches.Get[original];
                bool isBehaviour = typeof(Behaviour).IsAssignableFrom(original.DeclaringType);

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.operand is MemberInfo member)
                    {
                        bool isColor32 = false;
                        bool useInstance = isBehaviour && !original.IsStatic;

                        bool isHookableFieldInfo()
                        {
                            if (original.DeclaringType.IsAssignableFrom(member.DeclaringType))
                            {
                                if ((patchInfo.PatchFlags & PatchInfo.Flags.Field) != 0 && instruction.opcode.IsAny(OpCodes.Ldfld, OpCodes.Ldsfld) && member is FieldInfo fi)
                                {
                                    if (fi.FieldType == typeof(Color) || (isColor32 = fi.FieldType == typeof(Color32)))
                                    {
                                        useInstance &= !fi.IsStatic;
                                        return true;
                                    }
                                }
                            }

                            return false;
                        }

                        bool isHookableMethodBase()
                        {
                            if (member is MethodBase mb)
                            {
                                useInstance &= original.DeclaringType.IsAssignableFrom(mb.DeclaringType) && !mb.IsStatic;

                                if (instruction.opcode.IsAny(OpCodes.Call, OpCodes.Callvirt) && mb is MethodInfo mi && (mi.ReturnType == typeof(Color) || (isColor32 = mi.ReturnType == typeof(Color32))))
                                {
                                    if ((patchInfo.PatchFlags & PatchInfo.Flags.Call) != 0)
                                    {
                                        return !mi.IsSpecialName;
                                    }
                                    else if ((patchInfo.PatchFlags & PatchInfo.Flags.ConstColorProperty) != 0)
                                    {
                                        return mi.IsSpecialName && mi.IsStatic && mi.DeclaringType == mi.ReturnType && mi.GetParameters().Length == 0;
                                    }
                                }
                                else if ((patchInfo.PatchFlags & PatchInfo.Flags.Constructor) != 0 && instruction.opcode == OpCodes.Newobj && mb is ConstructorInfo ci && (ci.DeclaringType == typeof(Color) || (isColor32 = ci.DeclaringType == typeof(Color32))))
                                {
                                    return true;
                                }
                            }

                            return false;
                        }

                        if (isHookableFieldInfo() || isHookableMethodBase())
                        {
                            if (isColor32)
                                yield return new CodeInstruction(OpCodes.Call, Color32ToColor_MI);

                            if (useInstance)
                            {
                                yield return new CodeInstruction(OpCodes.Ldarg_0);
                                yield return new CodeInstruction(OpCodes.Call, ColorReplacer.tryGetReplacement_MI);
                            }
                            else
                            {
                                yield return new CodeInstruction(OpCodes.Call, ColorReplacer.tryGetGlobalReplacement_MI);
                            }

                            if (isColor32)
                                yield return new CodeInstruction(OpCodes.Call, ColorToColor32_MI);
                        }
                    }
                }
            }
        }
    }
}
