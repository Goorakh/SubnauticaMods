using GRandomizer.RandomizerControllers.Callbacks;
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
using UnityModdingUtility;
using UnityModdingUtility.Extensions;
using uSky;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class ColorRandomizer
    {
        static readonly MethodInfo IsEnabled_MI = SymbolExtensions.GetMethodInfo(() => IsEnabled());
        static bool IsEnabled()
        {
            return Mod.Config.RandomColors;
        }

        static void Reset()
        {
            ColorReplacer.Reset();
        }

        class ColorReplacer : MonoBehaviour
        {
            static Vector3 _globalColorOffset;
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
                if (_isInitialized && !_component.Exists())
                {
                    Destroy(this);
                }
            }

            static void SetGlobalColorOffset()
            {
                _globalColorOffset = new Vector3(UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f), UnityEngine.Random.Range(-1f, 1f));
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

            static Color rotateColor(Color original, Vector3 offset, float alpha)
            {
                return new Color(Mathf.Abs(Mathf.Sin((original.r + offset.x) * (2f * Mathf.PI))),
                                 Mathf.Abs(Mathf.Sin((original.g + offset.y) * (2f * Mathf.PI))),
                                 Mathf.Abs(Mathf.Sin((original.b + offset.z) * (2f * Mathf.PI))),
                                 float.IsNaN(alpha) ? original.a : alpha);
            }

            public Color GetReplacement(Color original, float alpha)
            {
                return rotateColor(original, _colorOffset, alpha);
            }
            public Color GetReplacement(Color original)
            {
                return GetReplacement(original, float.NaN);
            }

            public static Color GetReplacement(Color original, Behaviour component, float alpha)
            {
                return component.Exists() ? getOrAddComponent(component).GetReplacement(original, alpha) : GetGlobalReplacement(original, alpha);
            }
            public static Color GetReplacement(Color original, Behaviour component)
            {
                return GetReplacement(original, component, float.NaN);
            }

            public static void ReplaceColor(ref Color color, Behaviour component, float alpha)
            {
                color = GetReplacement(color, component, alpha);
            }
            public static void ReplaceColor(ref Color32 color, Behaviour component, float alpha)
            {
                color = GetReplacement(color, component, alpha);
            }

            public static readonly MethodInfo ReplaceColor_MI = SymbolExtensions.GetMethodInfo(() => ReplaceColor(ref Discard<Color>.Value, default));
            public static void ReplaceColor(ref Color color, Behaviour component)
            {
                ReplaceColor(ref color, component, float.NaN);
            }
            public static readonly MethodInfo ReplaceColor32_MI = SymbolExtensions.GetMethodInfo(() => ReplaceColor(ref Discard<Color32>.Value, default));
            public static void ReplaceColor(ref Color32 color, Behaviour component)
            {
                ReplaceColor(ref color, component, float.NaN);
            }

            public static readonly MethodInfo TryReplaceColor_MI = SymbolExtensions.GetMethodInfo(() => TryReplaceColor(ref Discard<Color>.Value, default));
            public static void TryReplaceColor(ref Color color, Behaviour component)
            {
                if (IsEnabled())
                {
                    ReplaceColor(ref color, component);
                }
            }
            public static readonly MethodInfo TryReplaceColor32_MI = SymbolExtensions.GetMethodInfo(() => TryReplaceColor(ref Discard<Color32>.Value, default));
            public static void TryReplaceColor(ref Color32 color, Behaviour component)
            {
                if (IsEnabled())
                {
                    ReplaceColor(ref color, component);
                }
            }

            public static readonly MethodInfo tryGetReplacement_MI = SymbolExtensions.GetMethodInfo(() => tryGetReplacement(default, default));
            static Color tryGetReplacement(Color original, Behaviour component)
            {
                return IsEnabled() ? GetReplacement(original, component) : original;
            }

            public static Color GetGlobalReplacement(Color original, float alpha)
            {
                return rotateColor(original, _globalColorOffset, alpha);
            }
            public static Color GetGlobalReplacement(Color original)
            {
                return GetGlobalReplacement(original, float.NaN);
            }

            public static void ReplaceColorGlobal(ref Color color, float alpha)
            {
                color = GetGlobalReplacement(color, alpha);
            }
            public static void ReplaceColorGlobal(ref Color32 color, float alpha)
            {
                color = GetGlobalReplacement(color, alpha);
            }

            public static readonly MethodInfo ReplaceColorGlobal_MI = SymbolExtensions.GetMethodInfo(() => ReplaceColorGlobal(ref Discard<Color>.Value));
            public static void ReplaceColorGlobal(ref Color color)
            {
                ReplaceColorGlobal(ref color, float.NaN);
            }
            public static readonly MethodInfo ReplaceColor32Global_MI = SymbolExtensions.GetMethodInfo(() => ReplaceColorGlobal(ref Discard<Color32>.Value));
            public static void ReplaceColorGlobal(ref Color32 color)
            {
                ReplaceColorGlobal(ref color, float.NaN);
            }

            public static readonly MethodInfo TryReplaceColorGlobal_MI = SymbolExtensions.GetMethodInfo(() => TryReplaceColorGlobal(ref Discard<Color>.Value));
            public static void TryReplaceColorGlobal(ref Color color)
            {
                if (IsEnabled())
                {
                    ReplaceColorGlobal(ref color);
                }
            }
            public static readonly MethodInfo TryReplaceColor32Global_MI = SymbolExtensions.GetMethodInfo(() => TryReplaceColorGlobal(ref Discard<Color32>.Value));
            public static void TryReplaceColorGlobal(ref Color32 color)
            {
                if (IsEnabled())
                {
                    ReplaceColorGlobal(ref color);
                }
            }

            public static readonly MethodInfo TryGetGlobalReplacement_MI = SymbolExtensions.GetMethodInfo(() => TryGetGlobalReplacement(default));
            public static Color TryGetGlobalReplacement(Color original)
            {
                return IsEnabled() ? GetGlobalReplacement(original) : original;
            }

            public static void Reset()
            {
                SetGlobalColorOffset();
            }

            public static void Initialize()
            {
                SetGlobalColorOffset();
            }
        }

        static void Initialize()
        {
            SceneManager.sceneLoaded += SceneManager_sceneLoaded;
            ColorReplacer.Initialize();
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

            randomizeLight(RenderSettings.sun);
        }

        static readonly MethodInfo randomizeLight_MI = SymbolExtensions.GetMethodInfo(() => randomizeLight(default));
        static void randomizeLight(Light light)
        {
            if (light.Exists())
            {
                light.color = Utils.Random.Color(light.color.a);
            }
        }

        static readonly MethodInfo randomizeGradient_MI = SymbolExtensions.GetMethodInfo(() => randomizeGradient(default, default));
        static void randomizeGradient(Gradient gradient, Behaviour behaviour)
        {
            GradientColorKey[] colorKeys = gradient.colorKeys;
            for (int i = 0; i < colorKeys.Length; i++)
            {
                colorKeys[i].color = ColorReplacer.GetReplacement(colorKeys[i].color, behaviour);
            }

            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;
            for (int i = 0; i < alphaKeys.Length; i++)
            {
                alphaKeys[i].alpha = UnityEngine.Random.value;
            }

            gradient.SetKeys(colorKeys, alphaKeys);
        }

        static readonly MethodInfo randomizeAnimationCurve_MI = SymbolExtensions.GetMethodInfo(() => randomizeAnimationCurve(default));
        static void randomizeAnimationCurve(AnimationCurve curve)
        {
            Keyframe[] keyframes = curve.keys;

            if (keyframes.Length > 0)
            {
                float minValue = keyframes.Min(k => k.m_Value);
                float maxValue = keyframes.Max(k => k.m_Value);

                for (int i = 0; i < keyframes.Length; i++)
                {
                    float time = keyframes[i].m_Time;

                    switch (UnityEngine.Random.Range(0, 4)) // [0,1,2,3]
                    {
                        case 0:
                            keyframes[i] = new Keyframe(time, UnityEngine.Random.Range(minValue, maxValue));
                            break;
                        case 1:
                            keyframes[i] = new Keyframe(time, UnityEngine.Random.Range(minValue, maxValue), UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-4f, 4f));
                            break;
                        case 2:
                            keyframes[i] = new Keyframe(time, UnityEngine.Random.Range(minValue, maxValue), UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.value, UnityEngine.Random.value)
                            {
                                m_WeightedMode = UnityEngine.Random.Range((int)WeightedMode.None, (int)WeightedMode.Both + 1)
                            };
                            break;
                        case 3:
                            break; // Do nothing
                    }
                }
            }

            curve.keys = keyframes;
        }

        static readonly ParticleSystemShapeType[] ParticleSystemShapeType_Options = new ParticleSystemShapeType[]
        {
            ParticleSystemShapeType.Sphere,
            ParticleSystemShapeType.Hemisphere,
            ParticleSystemShapeType.Cone,
            ParticleSystemShapeType.Box,
            ParticleSystemShapeType.ConeVolume,
            ParticleSystemShapeType.Circle,
            ParticleSystemShapeType.SingleSidedEdge,
            ParticleSystemShapeType.BoxShell,
            ParticleSystemShapeType.BoxEdge,
            ParticleSystemShapeType.Donut,
            ParticleSystemShapeType.Rectangle
        };

        static readonly ParticleSystemSimulationSpace[] ParticleSystemSimulationSpace_Options = new ParticleSystemSimulationSpace[]
        {
            ParticleSystemSimulationSpace.Local,
            ParticleSystemSimulationSpace.World
        };

        static void randomizeParticleSystem(ParticleSystem ps)
        {
#if !DEBUG
            return;
#endif

            ParticleSystem.MinMaxGradient randomizeMinMaxGradient(ParticleSystem.MinMaxGradient minmax)
            {
                switch (minmax.m_Mode)
                {
                    case ParticleSystemGradientMode.Color:
                        minmax.colorMax = ColorReplacer.GetGlobalReplacement(minmax.colorMax);
                        break;
                    case ParticleSystemGradientMode.Gradient:
                        randomizeGradient(minmax.m_GradientMax, null);
                        break;
                    case ParticleSystemGradientMode.TwoColors:
                        minmax.colorMin = ColorReplacer.GetGlobalReplacement(minmax.colorMin);
                        minmax.colorMax = ColorReplacer.GetGlobalReplacement(minmax.colorMax);
                        break;
                    case ParticleSystemGradientMode.TwoGradients:
                        randomizeGradient(minmax.m_GradientMin, null);
                        randomizeGradient(minmax.m_GradientMax, null);
                        break;
                }

                return minmax;
            }

            ParticleSystem.MinMaxCurve randomizeMinMaxCurve(ParticleSystem.MinMaxCurve minmax)
            {
                switch (minmax.m_Mode)
                {
                    case ParticleSystemCurveMode.Constant:
                        minmax.m_ConstantMax *= UnityEngine.Random.Range(0.5f, 2f);
                        break;
                    case ParticleSystemCurveMode.Curve:
                        minmax.m_CurveMultiplier *= UnityEngine.Random.Range(0.5f, 2f);
                        randomizeAnimationCurve(minmax.m_CurveMax);
                        break;
                    case ParticleSystemCurveMode.TwoCurves:
                        minmax.m_CurveMultiplier *= UnityEngine.Random.Range(0.5f, 2f);
                        randomizeAnimationCurve(minmax.m_CurveMin);
                        randomizeAnimationCurve(minmax.m_CurveMax);
                        break;
                    case ParticleSystemCurveMode.TwoConstants:
                        minmax.m_ConstantMin *= UnityEngine.Random.Range(0.5f, 2f);
                        minmax.m_ConstantMax *= Mathf.Max(minmax.m_ConstantMin, UnityEngine.Random.Range(0.5f, 2f));
                        break;
                }

                return minmax;
            }

#region main
            ParticleSystem.MainModule main = ps.main;

            main.duration *= UnityEngine.Random.Range(0.75f, 1.25f);

            main.startDelay = randomizeMinMaxCurve(main.startDelay);
            main.startDelayMultiplier *= UnityEngine.Random.Range(0.75f, 1.25f);

            main.startLifetime = randomizeMinMaxCurve(main.startLifetime);
            main.startLifetimeMultiplier *= UnityEngine.Random.Range(0.75f, 1.25f);

            main.startSpeed = randomizeMinMaxCurve(main.startSpeed);
            main.startSpeedMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

            if (main.startSize3D ^= Utils.Random.Boolean())
            {
                main.startSizeX = randomizeMinMaxCurve(main.startSizeX);
                main.startSizeXMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

                main.startSizeY = randomizeMinMaxCurve(main.startSizeY);
                main.startSizeYMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

                main.startSizeZ = randomizeMinMaxCurve(main.startSizeZ);
                main.startSizeZMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);
            }
            else
            {
                main.startSize = randomizeMinMaxCurve(main.startSize);
                main.startSizeMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);
            }

            if (main.startRotation3D ^= Utils.Random.Boolean())
            {
                main.startRotationX = randomizeMinMaxCurve(main.startRotationX);
                main.startRotationXMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

                main.startRotationY = randomizeMinMaxCurve(main.startRotationY);
                main.startRotationYMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

                main.startRotationZ = randomizeMinMaxCurve(main.startRotationZ);
                main.startRotationZMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);
            }
            else
            {
                main.startRotation = randomizeMinMaxCurve(main.startRotation);
                main.startRotationMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);
            }

            main.startColor = randomizeMinMaxGradient(main.startColor);

            main.gravityModifier = randomizeMinMaxCurve(main.gravityModifier);
            main.gravityModifierMultiplier *= UnityEngine.Random.Range(0.25f, 1.75f);

            main.simulationSpeed *= UnityEngine.Random.Range(0.3f, 1.7f);
#endregion

#region emission
            ParticleSystem.EmissionModule emission = ps.emission;
            if (emission.enabled ^= Utils.Random.Boolean(0.2f))
            {
                emission.rateOverTime = randomizeMinMaxCurve(emission.rateOverTime);
                emission.rateOverTimeMultiplier *= UnityEngine.Random.Range(0.75f, 1.25f);

                emission.rateOverDistance = randomizeMinMaxCurve(emission.rateOverDistance);
                emission.rateOverDistanceMultiplier *= UnityEngine.Random.Range(0.75f, 1.25f);
            }
#endregion

#region shape
            ParticleSystem.ShapeModule shape = ps.shape;
            bool shapeWasEnabled = shape.enabled;
            if (shape.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (!shapeWasEnabled)
                {
                    shape.shapeType = ParticleSystemShapeType_Options.GetRandom();
                }

                shape.randomDirectionAmount = UnityEngine.Random.Range(0f, 1f);
                shape.sphericalDirectionAmount = UnityEngine.Random.Range(0f, 1f);
                shape.randomPositionAmount = UnityEngine.Random.Range(0f, 1f);

                shape.alignToDirection = UnityEngine.Random.value >= 0.5f;

                shape.radius *= UnityEngine.Random.Range(0.1f, 4f);
                shape.radiusMode = Utils.Random.EnumValue<ParticleSystemShapeMultiModeValue>();
                shape.radiusSpread = UnityEngine.Random.Range(0f, 3f);

                shape.radiusSpeed = randomizeMinMaxCurve(shape.radiusSpeed);
                shape.radiusSpeedMultiplier *= UnityEngine.Random.Range(0.5f, 2f);

                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.Sphere:
                    case ParticleSystemShapeType.Cone:
                    case ParticleSystemShapeType.Circle:
                        shape.radiusThickness = UnityEngine.Random.Range(0f, 1f);
                        break;
                }

                shape.angle = UnityEngine.Random.Range(0f, 180f);

                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.ConeVolume:
                        shape.length = UnityEngine.Random.Range(0f, 6f);
                        break;
                }

                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.SingleSidedEdge:
                    case ParticleSystemShapeType.BoxShell:
                    case ParticleSystemShapeType.BoxEdge:
                        shape.boxThickness = Utils.Abs(UnityEngine.Random.insideUnitSphere) * UnityEngine.Random.Range(0.1f, 4f);
                        break;
                }

                if (shape.mesh.Exists() || shape.meshRenderer.Exists() || shape.skinnedMeshRenderer.Exists())
                {
                    shape.meshShapeType = Utils.Random.EnumValue<ParticleSystemMeshShapeType>();
                    shape.normalOffset *= UnityEngine.Random.Range(1f / 3f, 3f);
                    shape.meshSpawnMode = Utils.Random.EnumValue<ParticleSystemShapeMultiModeValue>();
                    shape.meshSpawnSpread = UnityEngine.Random.Range(0f, 3f);
                    shape.meshSpawnSpeed = randomizeMinMaxCurve(shape.meshSpawnSpeed);
                    shape.meshSpawnSpeedMultiplier *= UnityEngine.Random.Range(0.5f, 2f);
                }

                shape.arc = UnityEngine.Random.Range(0f, 180f);
                shape.arcMode = Utils.Random.EnumValue<ParticleSystemShapeMultiModeValue>();
                shape.arcSpread = UnityEngine.Random.Range(0f, 3f);
                shape.arcSpeed = randomizeMinMaxCurve(shape.arcSpeed);
                shape.arcSpeedMultiplier *= UnityEngine.Random.Range(0.5f, 2f);

                switch (shape.shapeType)
                {
                    case ParticleSystemShapeType.Donut:
                        shape.donutRadius = UnityEngine.Random.Range(0f, 5f);
                        break;
                }

                shape.position = UnityEngine.Random.insideUnitSphere * UnityEngine.Random.Range(0.1f, 5f);
                shape.rotation = Utils.Random.Rotation.eulerAngles;
                shape.scale = Utils.Abs(UnityEngine.Random.insideUnitSphere) * UnityEngine.Random.Range(0.1f, 4f);

                if (shape.texture.Exists())
                {
                    shape.textureClipChannel = Utils.Random.EnumValue<ParticleSystemShapeTextureChannel>();
                    shape.textureClipThreshold = UnityEngine.Random.value;
                }
            }
#endregion

#region velocityOverLifetime
            ParticleSystem.VelocityOverLifetimeModule velocityOverLifetime = ps.velocityOverLifetime;
            bool velocityOverLifetimeWasEnabled = velocityOverLifetime.enabled;
            if (velocityOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (!velocityOverLifetimeWasEnabled)
                {
                    velocityOverLifetime.space = ParticleSystemSimulationSpace_Options.GetRandom();
                }

                velocityOverLifetime.x = randomizeMinMaxCurve(velocityOverLifetime.x);
                velocityOverLifetime.y = randomizeMinMaxCurve(velocityOverLifetime.y);
                velocityOverLifetime.z = randomizeMinMaxCurve(velocityOverLifetime.z);

                velocityOverLifetime.xMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.yMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.zMultiplier = UnityEngine.Random.Range(0f, 3f);

                velocityOverLifetime.orbitalX = randomizeMinMaxCurve(velocityOverLifetime.orbitalX);
                velocityOverLifetime.orbitalY = randomizeMinMaxCurve(velocityOverLifetime.orbitalY);
                velocityOverLifetime.orbitalZ = randomizeMinMaxCurve(velocityOverLifetime.orbitalZ);

                velocityOverLifetime.orbitalXMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.orbitalYMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.orbitalZMultiplier = UnityEngine.Random.Range(0f, 3f);

                velocityOverLifetime.orbitalOffsetX = randomizeMinMaxCurve(velocityOverLifetime.orbitalOffsetX);
                velocityOverLifetime.orbitalOffsetY = randomizeMinMaxCurve(velocityOverLifetime.orbitalOffsetY);
                velocityOverLifetime.orbitalOffsetZ = randomizeMinMaxCurve(velocityOverLifetime.orbitalOffsetZ);

                velocityOverLifetime.orbitalOffsetXMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.orbitalOffsetYMultiplier = UnityEngine.Random.Range(0f, 3f);
                velocityOverLifetime.orbitalOffsetZMultiplier = UnityEngine.Random.Range(0f, 3f);

                velocityOverLifetime.radial = randomizeMinMaxCurve(velocityOverLifetime.radial);
                velocityOverLifetime.radialMultiplier = UnityEngine.Random.Range(0f, 3f);

                velocityOverLifetime.speedModifier = randomizeMinMaxCurve(velocityOverLifetime.speedModifier);
                velocityOverLifetime.speedModifierMultiplier = UnityEngine.Random.Range(0f, 3f);
            }
#endregion

#region limitVelocityOverLifetime
            ParticleSystem.LimitVelocityOverLifetimeModule limitVelocityOverLifetime = ps.limitVelocityOverLifetime;
            bool limitVelocityOverLifetimeWasEnabled = limitVelocityOverLifetime.enabled;
            if (limitVelocityOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (!limitVelocityOverLifetimeWasEnabled)
                {
                    limitVelocityOverLifetime.space = ParticleSystemSimulationSpace_Options.GetRandom();
                }

                if (limitVelocityOverLifetime.separateAxes ^= Utils.Random.Boolean())
                {
                    limitVelocityOverLifetime.limitX = randomizeMinMaxCurve(limitVelocityOverLifetime.limitX);
                    limitVelocityOverLifetime.limitXMultiplier = UnityEngine.Random.Range(0f, 3f);

                    limitVelocityOverLifetime.limitY = randomizeMinMaxCurve(limitVelocityOverLifetime.limitY);
                    limitVelocityOverLifetime.limitYMultiplier = UnityEngine.Random.Range(0f, 3f);

                    limitVelocityOverLifetime.limitZ = randomizeMinMaxCurve(limitVelocityOverLifetime.limitZ);
                    limitVelocityOverLifetime.limitZMultiplier = UnityEngine.Random.Range(0f, 3f);
                }
                else
                {
                    limitVelocityOverLifetime.limit = randomizeMinMaxCurve(limitVelocityOverLifetime.limit);
                    limitVelocityOverLifetime.limitMultiplier = UnityEngine.Random.Range(0f, 3f);
                }

                limitVelocityOverLifetime.dampen = UnityEngine.Random.value;

                limitVelocityOverLifetime.drag = randomizeMinMaxCurve(limitVelocityOverLifetime.drag);
                limitVelocityOverLifetime.dragMultiplier = UnityEngine.Random.Range(0f, 3f);
            }
#endregion

#region inheritVelocity
            ParticleSystem.InheritVelocityModule inheritVelocity = ps.inheritVelocity;
            if (inheritVelocity.enabled ^= Utils.Random.Boolean(0.2f))
            {
                inheritVelocity.mode = Utils.Random.EnumValue<ParticleSystemInheritVelocityMode>();

                inheritVelocity.curve = randomizeMinMaxCurve(inheritVelocity.curve);
                inheritVelocity.curveMultiplier = UnityEngine.Random.Range(0f, 3f);
            }
#endregion

#region forceOverLifetime
            ParticleSystem.ForceOverLifetimeModule forceOverLifetime = ps.forceOverLifetime;
            bool forceOverLifetimeWasEnabled = forceOverLifetime.enabled;
            if (forceOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (!forceOverLifetimeWasEnabled)
                {
                    forceOverLifetime.space = ParticleSystemSimulationSpace_Options.GetRandom();
                }

                forceOverLifetime.x = randomizeMinMaxCurve(forceOverLifetime.x);
                forceOverLifetime.y = randomizeMinMaxCurve(forceOverLifetime.y);
                forceOverLifetime.z = randomizeMinMaxCurve(forceOverLifetime.z);

                forceOverLifetime.xMultiplier = UnityEngine.Random.Range(0f, 2f);
                forceOverLifetime.yMultiplier = UnityEngine.Random.Range(0f, 2f);
                forceOverLifetime.zMultiplier = UnityEngine.Random.Range(0f, 2f);
            }
#endregion

#region colorOverLifetime
            ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
            if (colorOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                colorOverLifetime.color = randomizeMinMaxGradient(colorOverLifetime.color);
            }
#endregion

#region colorBySpeed
            ParticleSystem.ColorBySpeedModule colorBySpeed = ps.colorBySpeed;
            bool colorBySpeedWasEnabled = colorBySpeed.enabled;
            if (colorBySpeed.enabled ^= Utils.Random.Boolean(0.2f))
            {
                colorBySpeed.color = randomizeMinMaxGradient(colorBySpeed.color);

                if (!colorBySpeedWasEnabled)
                {
                    float min = UnityEngine.Random.Range(0f, 5f);
                    colorBySpeed.range = new Vector2(min, min + UnityEngine.Random.Range(0f, 5f));
                }
            }
#endregion

#region sizeOverLifetime
            ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = ps.sizeOverLifetime;
            if (sizeOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (sizeOverLifetime.separateAxes ^= Utils.Random.Boolean())
                {
                    sizeOverLifetime.x = randomizeMinMaxCurve(sizeOverLifetime.x);
                    sizeOverLifetime.xMultiplier = UnityEngine.Random.Range(0f, 2f);

                    sizeOverLifetime.y = randomizeMinMaxCurve(sizeOverLifetime.y);
                    sizeOverLifetime.yMultiplier = UnityEngine.Random.Range(0f, 2f);

                    sizeOverLifetime.z = randomizeMinMaxCurve(sizeOverLifetime.z);
                    sizeOverLifetime.zMultiplier = UnityEngine.Random.Range(0f, 2f);
                }
                else
                {
                    sizeOverLifetime.size = randomizeMinMaxCurve(sizeOverLifetime.size);
                    sizeOverLifetime.sizeMultiplier = UnityEngine.Random.Range(0f, 2f);
                }
            }
#endregion

#region sizeBySpeed
            ParticleSystem.SizeBySpeedModule sizeBySpeed = ps.sizeBySpeed;
            bool sizeBySpeedWasEnabled = sizeBySpeed.enabled;
            if (sizeBySpeed.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (sizeBySpeed.separateAxes ^= Utils.Random.Boolean())
                {
                    sizeBySpeed.x = randomizeMinMaxCurve(sizeBySpeed.x);
                    sizeBySpeed.xMultiplier = UnityEngine.Random.Range(0f, 2f);

                    sizeBySpeed.y = randomizeMinMaxCurve(sizeBySpeed.y);
                    sizeBySpeed.yMultiplier = UnityEngine.Random.Range(0f, 2f);

                    sizeBySpeed.z = randomizeMinMaxCurve(sizeBySpeed.z);
                    sizeBySpeed.zMultiplier = UnityEngine.Random.Range(0f, 2f);
                }
                else
                {
                    sizeBySpeed.size = randomizeMinMaxCurve(sizeBySpeed.size);
                    sizeBySpeed.sizeMultiplier = UnityEngine.Random.Range(0f, 2f);
                }

                if (!sizeBySpeedWasEnabled)
                {
                    float min = UnityEngine.Random.Range(0f, 5f);
                    sizeBySpeed.range = new Vector2(min, min + UnityEngine.Random.Range(0f, 5f));
                }
            }
#endregion

#region rotationOverLifetime
            ParticleSystem.RotationOverLifetimeModule rotationOverLifetime = ps.rotationOverLifetime;
            if (rotationOverLifetime.enabled ^= Utils.Random.Boolean(0.2f))
            {
                rotationOverLifetime.x = randomizeMinMaxCurve(rotationOverLifetime.x);
                rotationOverLifetime.xMultiplier = UnityEngine.Random.Range(0f, 2f);

                if (rotationOverLifetime.separateAxes ^= Utils.Random.Boolean())
                {
                    rotationOverLifetime.y = randomizeMinMaxCurve(rotationOverLifetime.y);
                    rotationOverLifetime.yMultiplier = UnityEngine.Random.Range(0f, 2f);

                    rotationOverLifetime.z = randomizeMinMaxCurve(rotationOverLifetime.z);
                    rotationOverLifetime.zMultiplier = UnityEngine.Random.Range(0f, 2f);
                }
            }
#endregion

#region rotationBySpeed
            ParticleSystem.RotationBySpeedModule rotationBySpeed = ps.rotationBySpeed;
            bool rotationBySpeedWasEnabled = rotationBySpeed.enabled;
            if (rotationBySpeed.enabled ^= Utils.Random.Boolean(0.2f))
            {
                rotationBySpeed.x = randomizeMinMaxCurve(rotationBySpeed.x);
                rotationBySpeed.xMultiplier = UnityEngine.Random.Range(0f, 2f);

                if (rotationBySpeed.separateAxes ^= Utils.Random.Boolean())
                {
                    rotationBySpeed.y = randomizeMinMaxCurve(rotationBySpeed.y);
                    rotationBySpeed.yMultiplier = UnityEngine.Random.Range(0f, 2f);

                    rotationBySpeed.z = randomizeMinMaxCurve(rotationBySpeed.z);
                    rotationBySpeed.zMultiplier = UnityEngine.Random.Range(0f, 2f);
                }

                if (!rotationBySpeedWasEnabled)
                {
                    float min = UnityEngine.Random.Range(0f, 5f);
                    rotationBySpeed.range = new Vector2(min, min + UnityEngine.Random.Range(0f, 5f));
                }
            }
#endregion

#region externalForces
            // TODO (maybe?)
#endregion

#region noise
            ParticleSystem.NoiseModule noise = ps.noise;
            if (noise.enabled ^= Utils.Random.Boolean(0.2f))
            {
                if (noise.separateAxes ^= Utils.Random.Boolean())
                {
                    noise.strengthX = randomizeMinMaxCurve(noise.strengthX);
                    noise.strengthXMultiplier = UnityEngine.Random.Range(0f, 2f);

                    noise.strengthY = randomizeMinMaxCurve(noise.strengthY);
                    noise.strengthYMultiplier = UnityEngine.Random.Range(0f, 2f);

                    noise.strengthZ = randomizeMinMaxCurve(noise.strengthZ);
                    noise.strengthZMultiplier = UnityEngine.Random.Range(0f, 2f);
                }
                else
                {
                    noise.strength = randomizeMinMaxCurve(noise.strength);
                    noise.strengthMultiplier = UnityEngine.Random.Range(0f, 2f);
                }

                noise.frequency = UnityEngine.Random.Range(0f, 2f);

                noise.damping ^= Utils.Random.Boolean();

                noise.scrollSpeed = randomizeMinMaxCurve(noise.scrollSpeed);
                noise.scrollSpeedMultiplier = UnityEngine.Random.Range(0f, 2f);

                if (noise.remapEnabled ^= Utils.Random.Boolean())
                {
                    noise.remap = randomizeMinMaxCurve(noise.remap);
                    noise.remapMultiplier = UnityEngine.Random.Range(0f, 2f);

                    noise.remapX = randomizeMinMaxCurve(noise.remapX);
                    noise.remapXMultiplier = UnityEngine.Random.Range(0f, 2f);

                    noise.remapY = randomizeMinMaxCurve(noise.remapY);
                    noise.remapYMultiplier = UnityEngine.Random.Range(0f, 2f);

                    noise.remapZ = randomizeMinMaxCurve(noise.remapZ);
                    noise.remapZMultiplier = UnityEngine.Random.Range(0f, 2f);
                }

                noise.positionAmount = randomizeMinMaxCurve(noise.positionAmount);
                noise.rotationAmount = randomizeMinMaxCurve(noise.rotationAmount);
                noise.sizeAmount = randomizeMinMaxCurve(noise.sizeAmount);
            }
#endregion

#region collision
            // TODO (maybe?)
#endregion

#region trigger
            // TODO (maybe?)
#endregion

#region subEmitters
            // TODO (maybe?)
#endregion

#region textureSheetAnimation
            ParticleSystem.TextureSheetAnimationModule textureSheetAnimation = ps.textureSheetAnimation;
            if (textureSheetAnimation.enabled)
            {
                switch (textureSheetAnimation.timeMode = Utils.Random.EnumValue<ParticleSystemAnimationTimeMode>())
                {
                    case ParticleSystemAnimationTimeMode.FPS:
                        textureSheetAnimation.fps = UnityEngine.Random.Range(0f, 60f);
                        break;
                }

                textureSheetAnimation.numTilesX = UnityEngine.Random.Range(1, 4);
                textureSheetAnimation.numTilesY = UnityEngine.Random.Range(1, 4);

                textureSheetAnimation.animation = Utils.Random.EnumValue<ParticleSystemAnimationType>();

                textureSheetAnimation.uvChannelMask = Utils.Random.EnumFlag<UVChannelFlags>();
            }
#endregion

#region lights
            ParticleSystem.LightsModule lights = ps.lights;
            if (lights.enabled && lights.light.Exists())
            {
                lights.ratio = UnityEngine.Random.value;
                lights.useRandomDistribution = Utils.Random.Boolean();

                randomizeLight(lights.light);

                lights.useParticleColor = Utils.Random.Boolean();
                lights.sizeAffectsRange = Utils.Random.Boolean();
                lights.alphaAffectsIntensity = Utils.Random.Boolean();

                lights.range = randomizeMinMaxCurve(lights.range);
                lights.rangeMultiplier = UnityEngine.Random.Range(0f, 2f);

                lights.intensity = randomizeMinMaxCurve(lights.intensity);
                lights.intensityMultiplier = UnityEngine.Random.Range(0f, 2f);

                lights.maxLights = UnityEngine.Random.Range(0, 100);
            }
#endregion

#region trails
            ParticleSystem.TrailModule trails = ps.trails;
            if (trails.enabled ^= Utils.Random.Boolean(0.2f))
            {
                trails.mode = Utils.Random.EnumValue<ParticleSystemTrailMode>();
                trails.ratio = UnityEngine.Random.value;

                trails.lifetime = randomizeMinMaxCurve(trails.lifetime);
                trails.lifetimeMultiplier = UnityEngine.Random.Range(0f, 2f);

                trails.minVertexDistance = UnityEngine.Random.Range(0f, 10f);

                trails.textureMode = Utils.Random.EnumValue<ParticleSystemTrailTextureMode>();

                trails.worldSpace = Utils.Random.Boolean();
                trails.dieWithParticles = Utils.Random.Boolean();
                trails.sizeAffectsWidth = Utils.Random.Boolean();
                trails.sizeAffectsLifetime = Utils.Random.Boolean();
                trails.inheritParticleColor = Utils.Random.Boolean();

                trails.colorOverLifetime = randomizeMinMaxGradient(trails.colorOverLifetime);

                trails.widthOverTrail = randomizeMinMaxCurve(trails.widthOverTrail);
                trails.widthOverTrailMultiplier = UnityEngine.Random.Range(0f, 2f);

                trails.colorOverTrail = randomizeMinMaxGradient(trails.colorOverTrail);

                trails.generateLightingData = Utils.Random.Boolean();

                trails.ribbonCount = UnityEngine.Random.Range(1, 5);

                trails.shadowBias = UnityEngine.Random.value;

                trails.splitSubEmitterRibbons = Utils.Random.Boolean();

                trails.attachRibbonsToTransform = Utils.Random.Boolean();
            }
#endregion
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
                    ColorReplacer.ReplaceColor(ref __instance.m_GroundColor, __instance);

                    __instance.skyFogDensity = Mathf.Pow(UnityEngine.Random.value, 5f) / 1000f;
                    randomizeGradient(__instance.skyFogColor, __instance);

                    __instance.planetRadius = UnityEngine.Random.Range(0f, 1000f);
                    __instance.planetZenith = UnityEngine.Random.Range(0f, 360f);
                    __instance.planetDistance *= UnityEngine.Random.Range(1f / 3f, 3f);
                    ColorReplacer.ReplaceColor(ref __instance.planetRimColor, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.planetAmbientLight, __instance);
                    __instance.planetOrbitSpeed *= UnityEngine.Random.Range(1f / 3f, 3f);
                    __instance.planetLightWrap = UnityEngine.Random.Range(0, 1f);
                    ColorReplacer.ReplaceColor(ref __instance.planetInnerCorona, __instance, UnityEngine.Random.value);
                    ColorReplacer.ReplaceColor(ref __instance.planetOuterCorona, __instance, UnityEngine.Random.value);

                    __instance.cloudsRotateSpeed *= UnityEngine.Random.Range(1f / 3f, 3f);
                    __instance.cloudNightBrightness = Mathf.Pow(UnityEngine.Random.Range(0f, 1f), 2f);
                    __instance.cloudsAttenuation = UnityEngine.Random.Range(0f, 1f);
                    __instance.cloudsAlphaSaturation = UnityEngine.Random.Range(1f, 10f);

                    __instance.sunColorMultiplier = UnityEngine.Random.Range(0f, 8f);

                    __instance.skyColorMultiplier = UnityEngine.Random.Range(0f, 8f);

                    __instance.cloudsScatteringMultiplier = UnityEngine.Random.Range(0f, 10f);
                    __instance.cloudsScatteringExponent = UnityEngine.Random.Range(0f, 30f);

                    randomizeGradient(__instance.meanSkyColor, __instance);

                    __instance.secondaryLightPow = UnityEngine.Random.Range(0f, 10f);

                    randomizeGradient(__instance.NightZenithColor, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.NightHorizonColor, __instance);
                    __instance.StarIntensity = UnityEngine.Random.Range(0f, 10f);

                    ColorReplacer.ReplaceColor(ref __instance.MoonInnerCorona, __instance, UnityEngine.Random.value);
                    ColorReplacer.ReplaceColor(ref __instance.MoonOuterCorona, __instance, UnityEngine.Random.value);
                    __instance.MoonSize = UnityEngine.Random.Range(0f, 1f);

                    __instance.directLightFraction = UnityEngine.Random.value;
                    __instance.indirectLightFraction = UnityEngine.Random.value;

                    __instance.endSequenceLightIntensity = UnityEngine.Random.Range(0f, 1.5f);
                    __instance.endSequenceSunSizeMultiplier = UnityEngine.Random.Range(0f, 1.5f);
                    __instance.endSequencePlanetRadius *= UnityEngine.Random.Range(0.7f, 1.3f);

                    ColorReplacer.ReplaceColor(ref __instance.endSequenceLightColor, __instance);
                    ColorReplacer.ReplaceColor(ref __instance.endSequenceSunBurstColor, __instance);

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

        [HarmonyPatch(typeof(WaterBiomeManager), "Start")]
        static class WaterBiomeManager_Start_Patch
        {
            static void Prefix(WaterBiomeManager __instance)
            {
                if (IsEnabled())
                {
                    List<GameObject> skyPrefabs = (from biome in __instance.biomeSettings
                                                   where biome != null && biome.skyPrefab.Exists()
                                                   select biome.skyPrefab).ToList();

                    foreach (WaterBiomeManager.BiomeSettings biome in __instance.biomeSettings)
                    {
                        if (biome != null)
                        {
                            if (biome.skyPrefab.Exists())
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
                            ColorReplacer.ReplaceColor(ref biome.settings.scatteringColor, __instance);
                            biome.settings.murkiness *= UnityEngine.Random.Range(0f, 2f);
                            ColorReplacer.ReplaceColor(ref biome.settings.emissive, __instance);
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
        static class WaterSurface_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<WaterSurface>(_ => _.Start());
            }

            static void Prefix(WaterSurface __instance)
            {
                if (!IsEnabled())
                    return;

#if VERBOSE
                Dictionary<FieldInfo, object> originalFieldValues = (from field in __instance.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                                     where !field.FieldType.IsClass
                                                                     select new KeyValuePair<FieldInfo, object>(field, field.GetValue(__instance))).ToDictionary();
#endif

                __instance.refractionIndex = UnityEngine.Random.Range(1f, 2f);
                __instance.underWaterRefractionIndex = UnityEngine.Random.Range(1f, 2f);
                __instance.underWaterRefractionDepthScale *= UnityEngine.Random.Range(0.5f, 2f);
                __instance.causticsFramesPerSecond = (int)(__instance.causticsFramesPerSecond * UnityEngine.Random.Range(1f / 3f, 3f));
                __instance.maxCausticsValue *= UnityEngine.Random.Range(1f / 2f, 2f);

                ColorReplacer.ReplaceColorGlobal(ref __instance.reflectionColor);
                ColorReplacer.ReplaceColorGlobal(ref __instance.refractionColor);
                ColorReplacer.ReplaceColorGlobal(ref __instance.backLightTint);

                __instance.sunReflectionGloss *= UnityEngine.Random.Range(1f / 3f, 3f);
                __instance.sunReflectionAmount *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.waveHeightThicknessScale *= UnityEngine.Random.Range(1f / 2f, 2f);

                __instance.foamSmoothing *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.foamRate *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.foamScale *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.foamDecay *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.foamDistance *= UnityEngine.Random.Range(1f / 2f, 2f);

                ColorReplacer.ReplaceColorGlobal(ref __instance.subSurfaceFoamColor);

                __instance.subSurfaceFoamScale *= UnityEngine.Random.Range(1f / 2f, 2f);

                if (__instance.sunLight)
                {
                    randomizeLight(__instance.sunLight);
                }

                __instance.pixelStrideZCutoff *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.pixelZSizeOffset *= UnityEngine.Random.Range(1f / 2f, 2f);
                __instance.maxRayDistance *= UnityEngine.Random.Range(1f / 2f, 2f);

                __instance.screenEdgeFadeStart = UnityEngine.Random.Range(0f, 1f);
                __instance.eyeFadeStart = UnityEngine.Random.Range(0f, 1f);
                __instance.eyeFadeEnd = UnityEngine.Random.Range(0f, 1f);
                __instance.screenSpaceRefractionIndex = UnityEngine.Random.Range(1f, 2f);
                __instance.screenSpaceInternalReflectionFlatness = UnityEngine.Random.Range(0f, 1f);

#if VERBOSE
                foreach (KeyValuePair<FieldInfo, object> original in originalFieldValues)
                {
                    object newValue = original.Key.GetValue(__instance);
                    if (!Equals(original.Value, newValue))
                    {
                        Utils.DebugLog($"WaterSurface.{original.Key.Name}: {original.Value} -> {newValue}");
                    }
                }
#endif
            }
        }

        [HarmonyPatch]
        static class uSkyLight_OnEnable_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uSkyLight>(_ => _.OnEnable());
            }

            static void Prefix(uSkyLight __instance)
            {
                if (IsEnabled())
                {
                    __instance.SunIntensity = UnityEngine.Random.Range(0f, 4f);
                    __instance.MoonIntensity = UnityEngine.Random.Range(0f, 2f);
                    __instance.ambientLight *= UnityEngine.Random.Range(0.5f, 2f);

                    randomizeGradient(__instance.Ambient.SkyColor, __instance);
                    randomizeGradient(__instance.Ambient.EquatorColor, __instance);
                    randomizeGradient(__instance.Ambient.GroundColor, __instance);
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
        static class GL_Color_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => GL.Color(default));
            }

            static void Prefix(ref Color c)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColorGlobal(ref c);
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
                        if (light.Exists())
                        {
                            randomizeLight(light);
                        }
                    }
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
        static class MainMenuPrimaryOption_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<MainMenuPrimaryOption>(_ => _.Start());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo Material_get_color_MI = AccessTools.DeclaredPropertyGetter(typeof(Material), nameof(Material.color));

                foreach (CodeInstruction instruction in instructions)
                {
                    yield return instruction;

                    if (instruction.Calls(Material_get_color_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg_0);
                        yield return new CodeInstruction(OpCodes.Call, ColorReplacer.tryGetReplacement_MI);
                    }
                }
            }
        }

        [HarmonyPatch]
        static class MapDisplay_GetColorForFraction_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<MapDisplay>(_ => _.GetColorForFraction(default));
            }

            static Color Postfix(Color __result, MapDisplay __instance)
            {
                if (IsEnabled())
                {
                    return ColorReplacer.GetReplacement(__result, __instance);
                }
                else
                {
                    return __result;
                }
            }
        }

        [HarmonyPatch]
        static class NightVision_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<NightVision>(_ => _.Start());
            }

            static void Prefix(NightVision __instance)
            {
                if (IsEnabled())
                {
                    __instance.noiseFactor *= UnityEngine.Random.Range(0.5f, 2f);
                }
            }
        }

        [HarmonyPatch]
        static class RadiationsScreenFX_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<RadiationsScreenFX>(_ => _.Start());
            }

            static void Prefix(RadiationsScreenFX __instance)
            {
                if (IsEnabled())
                {
                    __instance.noiseFactor *= UnityEngine.Random.Range(0.5f, 2f);
                }
            }
        }

        [HarmonyPatch]
        static class RocketPreflightCheckScreenElement_Start_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<RocketPreflightCheckScreenElement>(_ => _.Start());
            }

            static void Prefix(RocketPreflightCheckScreenElement __instance)
            {
                if (IsEnabled())
                {
                    __instance.localizedCheckText.color = ColorReplacer.GetReplacement(__instance.localizedCheckText.color, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class HookMaterialGetColor_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.DeclaredMethod(typeof(StasisSphere), nameof(StasisSphere.Awake));
                yield return SymbolExtensions.GetMethodInfo<TelepathyScreenFX>(_ => _.SpawnGhost());
                yield return SymbolExtensions.GetMethodInfo<Trail_v2>(_ => _.Awake());
                yield return SymbolExtensions.GetMethodInfo<SubFire>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<VehicleInterface_Terrain>(_ => _.InitializeHologram());
                yield return SymbolExtensions.GetMethodInfo<VFXExtinguishableFire.FireElement>(_ => _.Init());
                yield return SymbolExtensions.GetMethodInfo<VFXSeamothDamages>(_ => _.Start());
                yield return SymbolExtensions.GetMethodInfo<VFXSpotlight>(_ => _.Initialize());
                yield return SymbolExtensions.GetMethodInfo<VFXTechLight>(_ => _.Awake());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
            {
                return instructions.HookGetMaterialValue<Color>(new LocalGenerator(generator), ColorReplacer.TryGetGlobalReplacement);
            }
        }

        [HarmonyPatch]
        static class IBuilderGhostModel_UpdateGhostModelColor_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return typeof(IBuilderGhostModel).GetImplementations(false, SymbolExtensions.GetMethodInfo<IBuilderGhostModel>(_ => _.UpdateGhostModelColor(default, ref Discard<Color>.Value)));
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
            {
                bool useInstance = !original.IsStatic && typeof(Behaviour).IsAssignableFrom(original.DeclaringType);
                int colorArgIndex = original.FindArgumentIndex(typeof(Color).MakeByRefType());

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.opcode == OpCodes.Ret)
                    {
                        yield return new CodeInstruction(OpCodes.Ldarg, colorArgIndex);

                        if (useInstance)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            yield return new CodeInstruction(OpCodes.Call, ColorReplacer.TryReplaceColor_MI);
                        }
                        else
                        {
                            yield return new CodeInstruction(OpCodes.Call, ColorReplacer.TryReplaceColorGlobal_MI);
                        }
                    }

                    yield return instruction;
                }
            }
        }

        [HarmonyPatch]
        static class ColorBlock_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                return from m in typeof(ColorBlock).GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)
                       where m.ReturnType == typeof(Color) && m.GetParameters().Length == 0
                       select m;
            }

            static Color Postfix(Color __result)
            {
                return IsEnabled() ? ColorReplacer.GetGlobalReplacement(__result) : __result;
            }
        }

        [HarmonyPatch]
        static class uGUI_ItemsContainer_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uGUI_ItemsContainer>(_ => _.Awake());
            }

            static bool _hasReplacedColors = false;

            static readonly FieldInfo containerColors_FI = AccessTools.DeclaredField(typeof(uGUI_ItemsContainer), nameof(uGUI_ItemsContainer.containerColors));

            static void Prefix()
            {
                if (IsEnabled() && !_hasReplacedColors)
                {
                    Dictionary<ItemsContainerType, Color> containerColors = (Dictionary<ItemsContainerType, Color>)containerColors_FI.GetValue(null);

                    Dictionary<ItemsContainerType, Color> newContainerColors = new Dictionary<ItemsContainerType, Color>();
                    foreach (ItemsContainerType containerType in containerColors.Keys)
                    {
                        newContainerColors[containerType] = ColorReplacer.GetGlobalReplacement(containerColors[containerType]);
                    }

                    containerColors_FI.SetValue(null, newContainerColors);

                    _hasReplacedColors = true;
                }
            }
        }

        [HarmonyPatch]
        static class uSky_StarField_InitializeStarfield_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<uSky.StarField>(_ => _.InitializeStarfield());
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo Mesh_set_colors_MI = AccessTools.DeclaredPropertySetter(typeof(Mesh), nameof(Mesh.colors));

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.Calls(Mesh_set_colors_MI))
                    {
                        yield return new CodeInstruction(OpCodes.Call, Hooks.Mesh_set_colors_MI);
                    }

                    yield return instruction;
                }
            }

            static class Hooks
            {
                public static readonly MethodInfo Mesh_set_colors_MI = SymbolExtensions.GetMethodInfo(() => Mesh_set_colors(default));
                static Color[] Mesh_set_colors(Color[] original)
                {
                    if (IsEnabled())
                    {
                        for (int i = 0; i < original.Length; i++)
                        {
                            original[i] = ColorReplacer.GetGlobalReplacement(original[i]);
                        }
                    }

                    return original;
                }
            }
        }

        [HarmonyPatch]
        static class Utils_DrawOutline_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => global::Utils.DrawOutline(default, default, default, default, default, default));
            }

            static void Prefix(ref Color outColor, ref Color inColor)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColorGlobal(ref outColor);
                    ColorReplacer.ReplaceColorGlobal(ref inColor);
                }
            }
        }

        [HarmonyPatch]
        static class Utils_DrawShadow_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo(() => global::Utils.DrawShadow(default, default, default, default, default, default));
            }

            static void Prefix(ref Color txtColor, ref Color shadowColor)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColorGlobal(ref txtColor);
                    ColorReplacer.ReplaceColorGlobal(ref shadowColor);
                }
            }
        }

        [HarmonyPatch]
        static class VFXOverlayMaterial_ApplyAndForgetOverlay_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<VFXOverlayMaterial>(_ => _.ApplyAndForgetOverlay(default, default, default, default));
            }

            static void Prefix(VFXOverlayMaterial __instance, ref Color lerpToColor)
            {
                if (IsEnabled())
                {
                    ColorReplacer.ReplaceColor(ref lerpToColor, __instance);
                }
            }
        }

        [HarmonyPatch]
        static class LightingController_Update_Patch
        {
            static MethodInfo TargetMethod()
            {
                return SymbolExtensions.GetMethodInfo<LightingController>(_ => _.Update());
            }

            static readonly HashSet<int> _randomizerLightingControllers = new HashSet<int>();
            static void Prefix(LightingController __instance)
            {
                if (IsEnabled() && _randomizerLightingControllers.Add(__instance.GetInstanceID()))
                {
                    if (__instance.lights != null)
                    {
                        foreach (MultiStatesLight light in __instance.lights)
                        {
                            randomizeLight(light.light);
                        }
                    }
                }
            }
        }

        [HarmonyPatch]
        static class Generic_ReplaceColorField_Patch
        {
            struct PatchInfo
            {
                public readonly MethodBase[] PatchTargets;
                public readonly FieldInfo[] StaticFields;
                public readonly FieldInfo[] InstanceFields;
                public int StaticHasReplacedIndex;

                public PatchInfo(MethodBase patchTarget, params string[] fieldNames) : this(new MethodBase[] { patchTarget }, fieldNames)
                {
                }

                public PatchInfo(MethodBase[] patchTargets, params string[] fieldNames) : this(patchTargets, fieldNames.Select(name => AccessTools.DeclaredField(patchTargets.First().DeclaringType, name)).ToArray())
                {
                    Type firstType = patchTargets.First().DeclaringType;
                    if (!patchTargets.All(m => m.DeclaringType == firstType))
                    {
                        throw new ArgumentException($"Not all methods have the same declaring type: {string.Join(", ", patchTargets.Select(m => m.FullName))}");
                    }
                }

                public PatchInfo(MethodBase[] patchTargets, params FieldInfo[] fields)
                {
                    PatchTargets = patchTargets;
                    StaticFields = fields.Where(f => f.IsStatic).ToArray();
                    InstanceFields = fields.Where(f => !f.IsStatic).ToArray();
                    StaticHasReplacedIndex = -1;
                }
            }

            static readonly FieldInfo _hasReplacedValues_FI = AccessTools.DeclaredField(typeof(Generic_ReplaceColorField_Patch), nameof(_hasReplacedValues));
            static bool[] _hasReplacedValues;

            static readonly InitializeOnAccess<Dictionary<MethodBase, PatchInfo>> _patches = new InitializeOnAccess<Dictionary<MethodBase, PatchInfo>>(() =>
            {
                PatchInfo[] patches = new PatchInfo[]
                {
                    new PatchInfo(SymbolExtensions.GetMethodInfo<uGUI_LogEntry>(_ => _.Initialize(default)), nameof(uGUI_LogEntry.buttonColorDefault), nameof(uGUI_LogEntry.buttonColorNotification), nameof(uGUI_LogEntry.backgroundColorDefault), nameof(uGUI_LogEntry.backgroundColorNotification), nameof(uGUI_LogEntry.iconColorDefault), nameof(uGUI_LogEntry.iconColorNotification)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<uSkyLight>(_ => _.OnEnable()), nameof(uSkyLight.LightColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<uFogGradient>(_ => _.Start()), nameof(uFogGradient.FogColor)),
                    new PatchInfo(AccessTools.DeclaredMethod(typeof(uGUI_GalleryTab), nameof(uGUI_GalleryTab.Awake)), nameof(uGUI_GalleryTab.colorNormal), nameof(uGUI_GalleryTab.colorHover), nameof(uGUI_GalleryTab.colorPress)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<uGUI_DepthCompass>(_ => _.Start()), nameof(uGUI_DepthCompass.textColorNormal), nameof(uGUI_DepthCompass.textColorDanger)),
                    new PatchInfo(AccessTools.DeclaredMethod(typeof(uGUI_Bar), nameof(uGUI_Bar.Awake)), nameof(uGUI_Bar.colorIcon), nameof(uGUI_Bar.colorBar)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SwitchColorChange>(_ => _.Start()), nameof(SwitchColorChange.startColor), nameof(SwitchColorChange.endColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SubFloodAlarm>(_ => _.Awake()), nameof(SubFloodAlarm.redAlarm), nameof(SubFloodAlarm.blueAlarm)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<VFXAnimator>(_ => _.Awake()), nameof(VFXAnimator.colorEnd)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SonarVision>(_ => _.Awake()), nameof(SonarVision.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<ScannerTool>(_ => _.Start()), nameof(ScannerTool.scanCircuitColor), nameof(ScannerTool.scanOrganicColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<RadiationsScreenFX>(_ => _.Start()), nameof(RadiationsScreenFX.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<PlaceTool>(_ => _.LateUpdate()), nameof(PlaceTool.placeColorAllow), nameof(PlaceTool.placeColorDeny)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<NotificationManager>(_ => _.Awake()), nameof(NotificationManager.notificationColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<NightVision>(_ => _.Start()), nameof(NightVision.luminence)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<mset.Logo>(_ => _.Start()), nameof(mset.Logo.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<MapRoomCameraScreenFX>(_ => _.Awake()), nameof(MapRoomCameraScreenFX.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<MapRoomCamera>(_ => _.Start()), nameof(MapRoomCamera.gradientInner), nameof(MapRoomCamera.gradientOuter)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<GUITextShadow>(_ => _.Start()), nameof(GUITextShadow.shadowColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<GenericConsole>(_ => _.Start()), nameof(GenericConsole.colorUsed), nameof(GenericConsole.colorUnused)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<FakeSunShafts>(_ => _.Start()), nameof(FakeSunShafts.beamColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<ExplosionScreenFX>(_ => _.Start()), nameof(ExplosionScreenFX.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<DiveReel>(_ => _.Start()), nameof(DiveReel.baseColor), nameof(DiveReel.lowAmmoColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<CyclopsSmokeScreenFX>(_ => _.Start()), nameof(CyclopsSmokeScreenFX.color)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<CyclopsHUDSonarPing>(_ => _.Start()), nameof(CyclopsHUDSonarPing.passiveColor), nameof(CyclopsHUDSonarPing.aggressiveColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<Charger>(_ => _.Awake()), nameof(Charger.colorEmpty), nameof(Charger.colorHalf), nameof(Charger.colorFull)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<BloomCreature>(_ => _.Awake()), nameof(BloomCreature.attractColor)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo(() => PingManager.NotifyColor(default)), nameof(PingManager.colorOptions)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<VFXLerpColor>(_ => _.Awake()), nameof(VFXLerpColor.colorEnd)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<CyclopsSmokeScreenFXController>(_ => _.Start()), nameof(CyclopsSmokeScreenFXController.intensityRemapCurve)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<DayNightLight>(_ => _.Awake()), nameof(DayNightLight.colorR), nameof(DayNightLight.colorG), nameof(DayNightLight.colorB), nameof(DayNightLight.intensity), nameof(DayNightLight.sunFraction), nameof(DayNightLight.replaceColor), nameof(DayNightLight.light)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<EcoManager>(_ => _.Start()), nameof(EcoManager.kSeasonalPhytoPlankton)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<EMPBlast>(_ => _.Start()), nameof(EMPBlast.blastRadius), nameof(EMPBlast.blastHeight)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<EndCreditsManager>(_ => _.Start()), nameof(EndCreditsManager.fadeCurve)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<EnergyEffect>(_ => _.Start()), nameof(EnergyEffect.powerAnim)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<EscapePodCinematicControl>(_ => _.OnIntroStart()), nameof(EscapePodCinematicControl.skyIntensityCurve)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SeamothTorpedoWhirlpool>(_ => _.Awake()), nameof(SeamothTorpedoWhirlpool.explosion), nameof(SeamothTorpedoWhirlpool.rotation)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SubFire>(_ => _.Start()), nameof(SubFire.smokeImpostorRemap)),
                    new PatchInfo(SymbolExtensions.GetMethodInfo<SunOrbit>(_ => _.Awake()), nameof(SunOrbit.dayNightCurve)),
                };

                _hasReplacedValues = new bool[patches.Length];

                int staticFieldsCount = 0;

                Dictionary<MethodBase, PatchInfo> dict = new Dictionary<MethodBase, PatchInfo>();
                for (int i = 0; i < patches.Length; i++)
                {
                    if (patches[i].StaticFields.Length > 0)
                        patches[i].StaticHasReplacedIndex = staticFieldsCount++;

                    foreach (MethodBase patchTarget in patches[i].PatchTargets)
                    {
                        dict[patchTarget] = patches[i];
                    }
                }

                return dict;
            });

            static IEnumerable<MethodBase> TargetMethods()
            {
                return _patches.Get.Keys;
            }

            public static readonly Dictionary<Type, MethodInfo> HookMethodsByType = new Dictionary<Type, MethodInfo>
            {
                {
                    typeof(Color),
                    ColorReplacer.ReplaceColor_MI
                },
                {
                    typeof(Color32),
                    ColorReplacer.ReplaceColor32_MI
                },
                {
                    typeof(Gradient),
                    randomizeGradient_MI
                },
                {
                    typeof(AnimationCurve),
                    randomizeAnimationCurve_MI
                },
                {
                    typeof(Light),
                    randomizeLight_MI
                }
            };

            static readonly MethodInfo replaceArrayValues_MI = AccessTools.DeclaredMethod(typeof(Generic_ReplaceColorField_Patch), nameof(replaceArrayValues));
            static void replaceArrayValues<T>(Array array, Behaviour instance)
            {
                MethodInfo hookMethod = HookMethodsByType[typeof(T)];
                bool useInstance = hookMethod.GetParameters().Last().ParameterType == typeof(Behaviour);

                for (int i = 0; i < array.Length; i++)
                {
                    object value = array.GetValue(i);
                    array.SetValue(hookMethod.Invoke(null, useInstance ? new object[] { value, instance } : new object[] { value }), i);
                }
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator generator)
            {
                PatchInfo patchInfo = _patches.Get[original];
                bool isUsableBehaviour = typeof(Behaviour).IsAssignableFrom(original.DeclaringType) && !original.IsConstructor && !original.IsStatic;

                List<CodeInstruction> prefix = new List<CodeInstruction>();

                Label skipPrefix = generator.DefineLabel();

                // if (!IsEnabled())
                prefix.Add(new CodeInstruction(OpCodes.Call, IsEnabled_MI));
                prefix.Add(new CodeInstruction(OpCodes.Brfalse, skipPrefix));

                IEnumerable<CodeInstruction> callHook(Type fieldType, bool includeInstance)
                {
                    MethodInfo hookMethod = fieldType.IsArray ? replaceArrayValues_MI.MakeGenericMethod(fieldType.GetElementType()) : HookMethodsByType[fieldType];

                    if (hookMethod.GetParameters().Last().ParameterType == typeof(Behaviour))
                        yield return new CodeInstruction(includeInstance ? OpCodes.Ldarg_0 : OpCodes.Ldnull);

                    yield return new CodeInstruction(OpCodes.Call, hookMethod);
                }

                if (patchInfo.StaticHasReplacedIndex != -1)
                {
                    Label skipStaticFields = generator.DefineLabel();

                    // if (!_hasReplacedValues[patchInfo.HasReplacedIndex])
                    prefix.Add(new CodeInstruction(OpCodes.Ldsfld, _hasReplacedValues_FI));
                    prefix.Add(new CodeInstruction(OpCodes.Ldc_I4, patchInfo.StaticHasReplacedIndex));
                    prefix.Add(new CodeInstruction(OpCodes.Ldelem_U1));
                    prefix.Add(new CodeInstruction(OpCodes.Brtrue, skipStaticFields));

                    foreach (FieldInfo field in patchInfo.StaticFields)
                    {
                        prefix.Add(new CodeInstruction(field.FieldType.IsValueType ? OpCodes.Ldsflda : OpCodes.Ldsfld, field));
                        prefix.AddRange(callHook(field.FieldType, false));
                    }

                    // _hasReplacedValues[patchInfo.HasReplacedIndex] = true;
                    prefix.Add(new CodeInstruction(OpCodes.Ldsfld, _hasReplacedValues_FI));
                    prefix.Add(new CodeInstruction(OpCodes.Ldc_I4, patchInfo.StaticHasReplacedIndex));
                    prefix.Add(new CodeInstruction(OpCodes.Ldc_I4_1));
                    prefix.Add(new CodeInstruction(OpCodes.Stelem_I1));

                    prefix.Add(new CodeInstruction(OpCodes.Nop).WithLabels(skipStaticFields));
                }

                foreach (FieldInfo field in patchInfo.InstanceFields)
                {
                    prefix.Add(new CodeInstruction(OpCodes.Ldarg_0));
                    prefix.Add(new CodeInstruction(field.FieldType.IsValueType ? OpCodes.Ldflda : OpCodes.Ldfld, field));
                    prefix.AddRange(callHook(field.FieldType, isUsableBehaviour));
                }

                prefix.Add(new CodeInstruction(OpCodes.Nop).WithLabels(skipPrefix));

                return Enumerable.Concat(prefix, instructions);
            }
        }

        [HarmonyPatch]
        static class Generic_ReplaceColorValue_Patch
        {
            struct PatchInfo
            {
                [Flags]
                public enum Flags : byte
                {
                    None = 0,
                    LoadField = 1 << 0, // Replace value of all Ld(s)fld instructions where the field type is Color or Color32
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
                    new PatchInfo(PatchInfo.Flags.LoadField, SymbolExtensions.GetMethodInfo(() => Builder.Update())),
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
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuContinueGameHandler>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuLoadButton>(_ => _.onCursorEnter())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuLoadButton>(_ => _.onCursorLeave())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuLoadMenu>(_ => _.DeselectItem())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuLoadMenu>(_ => _.SelectItem(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<MainMenuLoadPanel>(_ => _.UpdateLoadButtonState(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<MainMenuPrimaryOption>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(MapDisplay))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<MemoryWarning>(_ => _.CheckMemory())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<OVRGridCube>(_ => _.CreateCubeGrid())),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(OVRPlatformMenu))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(OVRScreenFade))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(PDAMap))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<PlayerTimeCapsule>(_ => _.OnSubmitResult(default, default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<PrecursorActivatedPillar>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<PrecursorActivatedPillar>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<PrecursorDisableGunTerminal>(_ => _.SetLightAccessGranted())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<PrecursorDisableGunTerminal>(_ => _.SetLightAccessDenied())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<RocketPreflightCheckScreenElement>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<RocketPreflightCheckScreenElement>(_ => _.SetPreflightCheckComplete(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<RocketPreflightCheckScreenElement>(_ => _.SetPreflightCheckIncomplete(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<ScannerTool>(_ => _.UpdateScreen(default, default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<StasisSphere>(_ => _.UpdateMaterials())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<TelepathyScreenFX>(_ => _.UpdateGhostMaterials())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<Terraformer>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<TerrainDebugGUI>(_ => _.LayoutPerfGUI())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<TestDrone>(_ => _.OnEcoEvent(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<TestVertexColors>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<ThermalPlant>(_ => _.UpdateUI())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(TopographicDisplay))),
                    new PatchInfo(PatchInfo.Flags.Call, SymbolExtensions.GetMethodInfo<Transfuser>(_ => _.CreateHeldSerum(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(TwoPointLine))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<SubFire>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_BasicColorSwap>(_ => _.makeTextBlack())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_BasicColorSwap>(_ => _.makeTextWhite())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_BindingText>(_ => _.SetColor(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(uGUI_CircularBar))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_Compass>(_ => _.UpdateLabels())),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(uGUI_EquipmentSlot))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_EscapePod>(_ => _.Awake())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.FeedbackUpdate())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.MessageFeedbackSent())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.MessageStreamingError())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_FeedbackCollector>(_ => _.OnValueChange(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(uGUI_FeedbackCollector))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(uGUI_IconGrid))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(uGUI_ItemIcon))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_MapRoomCancel>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_MapRoomCancel>(_ => _.OnPointerEnter(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_MapRoomCancel>(_ => _.OnPointerExit(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_MapRoomCancel>(_ => _.OnPointerClick(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(uGUI_MapRoomScanner))),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerDeath>(_ => _.CutToBlack())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerDeath>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerDeath>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerSleep>(_ => _.BeginFadeIn())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerSleep>(_ => _.Start())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<uGUI_PlayerSleep>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_PowerIndicator>(_ => _.UpdatePower())),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(uGUI_SignInput))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(uGUI_TimeCapsuleTab))),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<VehicleInterface_EnergyBar>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<VehicleInterface_GlowEffect>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, AccessTools.DeclaredConstructor(typeof(VehicleInterface_MapController))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<VehicleInterface_MapController>(_ => _.Update())),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(VFXConstructing))),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<uGUI_LogEntry>(_ => _.Initialize(default))),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredMethod(typeof(uGUI_PopupNotification), nameof(uGUI_PopupNotification.Awake))),
                    new PatchInfo(PatchInfo.Flags.Constructor | PatchInfo.Flags.ConstColorProperty, AccessTools.EnumeratorMoveNext(SymbolExtensions.GetMethodInfo<uGUI_SceneIntro>(_ => _.IntroSequence()))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty | PatchInfo.Flags.LoadField, SymbolExtensions.GetMethodInfo<VFXPrecursorGunElevator>(_ => _.UpdateWallLights())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<VFXSandSharkDune>(_ => _.Awake())),
                    new PatchInfo(PatchInfo.Flags.Constructor, SymbolExtensions.GetMethodInfo<VFXScan>(_ => _.StartScan(default))),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<VFXSunbeam>(_ => _.UpdateSequence())),
                    new PatchInfo(PatchInfo.Flags.ConstColorProperty, SymbolExtensions.GetMethodInfo<VFXSunbeam>(_ => _.GetCloudsColor())),
                    new PatchInfo(PatchInfo.Flags.Constructor, AccessTools.DeclaredConstructor(typeof(VFXWeatherManager))),
                }.ToDictionary(p => p.Target);
            });

            static IEnumerable<MethodBase> TargetMethods()
            {
                return _patches.Get.Keys;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original, ILGenerator generator)
            {
                MethodInfo ColorToColor32_MI = AccessTools.DeclaredMethod(typeof(Color32), "op_Implicit", new Type[] { typeof(Color) });
                MethodInfo Color32ToColor_MI = AccessTools.DeclaredMethod(typeof(Color32), "op_Implicit", new Type[] { typeof(Color32) });

                PatchInfo patchInfo = _patches.Get[original];
                bool isUsableBehaviour = typeof(Behaviour).IsAssignableFrom(original.DeclaringType) && !original.IsConstructor && !original.IsStatic;

                LocalGenerator localGen = new LocalGenerator(generator);

                foreach (CodeInstruction instruction in instructions)
                {
                    if (instruction.operand is MemberInfo member)
                    {
                        bool isColor32 = false;
                        bool useInstance = isUsableBehaviour;
                        bool isCallConstructor = false;

                        bool isHookableFieldInfo()
                        {
                            if (original.DeclaringType.IsAssignableFrom(member.DeclaringType))
                            {
                                if ((patchInfo.PatchFlags & PatchInfo.Flags.LoadField) != 0 && instruction.opcode.IsAny(OpCodes.Ldfld, OpCodes.Ldsfld) && member is FieldInfo fi)
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
                                else if ((patchInfo.PatchFlags & PatchInfo.Flags.Constructor) != 0 && mb is ConstructorInfo ci && (ci.DeclaringType == typeof(Color) || (isColor32 = ci.DeclaringType == typeof(Color32))))
                                {
                                    if (instruction.opcode == OpCodes.Newobj || (isCallConstructor = instruction.opcode == OpCodes.Call))
                                    {
                                        return true;
                                    }
                                }
                            }

                            return false;
                        }

                        if (isHookableMethodBase() || isHookableFieldInfo())
                        {
                            if (isCallConstructor)
                            {
                                ConstructorInfo constructor = (ConstructorInfo)instruction.operand;

                                ParameterInfo[] constructorParameters = constructor.GetParameters();
                                LocalBuilder[] parameterLocals = new LocalBuilder[constructorParameters.Length];
                                for (int i = constructorParameters.Length - 1; i >= 0; i--)
                                {
                                    yield return new CodeInstruction(OpCodes.Stloc, parameterLocals[i] = localGen.GetLocal(constructorParameters[i].ParameterType, false));
                                }

                                yield return new CodeInstruction(OpCodes.Dup); // Dup instance (ref)

                                for (int i = 0; i < parameterLocals.Length; i++)
                                {
                                    yield return new CodeInstruction(OpCodes.Ldloc, parameterLocals[i]);
                                    localGen.ReleaseLocal(parameterLocals[i]);
                                }

                                yield return instruction;

                                yield return new CodeInstruction(useInstance ? OpCodes.Ldarg_0 : OpCodes.Ldnull);
                                yield return new CodeInstruction(OpCodes.Call, isColor32 ? ColorReplacer.TryReplaceColor32_MI : ColorReplacer.TryReplaceColor_MI);
                            }
                            else
                            {
                                yield return instruction;

                                if (isColor32)
                                    yield return new CodeInstruction(OpCodes.Call, Color32ToColor_MI);

                                if (useInstance)
                                {
                                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                                    yield return new CodeInstruction(OpCodes.Call, ColorReplacer.tryGetReplacement_MI);
                                }
                                else
                                {
                                    yield return new CodeInstruction(OpCodes.Call, ColorReplacer.TryGetGlobalReplacement_MI);
                                }

                                if (isColor32)
                                    yield return new CodeInstruction(OpCodes.Call, ColorToColor32_MI);
                            }
                        }
                        else
                        {
                            yield return instruction;
                        }
                    }
                    else
                    {
                        yield return instruction;
                    }
                }
            }
        }

        [HarmonyPatch]
        static class ParticleSystem_Patch
        {
            static IEnumerable<MethodInfo> TargetMethods()
            {
                yield return SymbolExtensions.GetMethodInfo<ParticleSystem>(_ => _.Play(default));
                yield return SymbolExtensions.GetMethodInfo<ParticleSystem>(_ => _.Simulate(default, default, default, default));
                yield return SymbolExtensions.GetMethodInfo<ParticleSystem>(_ => _.Emit_Internal(default));
                yield return SymbolExtensions.GetMethodInfo<ParticleSystem>(_ => _.EmitOld_Internal(ref Discard<ParticleSystem.Particle>.Value));
                yield return SymbolExtensions.GetMethodInfo<ParticleSystem>(_ => _.Emit_Injected(ref Discard<ParticleSystem.EmitParams>.Value, default));
            }

            static readonly HashSet<int> _randomizedParticleSystems = new HashSet<int>();

            static void Prefix(ParticleSystem __instance)
            {
                if (IsEnabled() && _randomizedParticleSystems.Add(__instance.GetInstanceID()))
                {
                    randomizeParticleSystem(__instance);
                }
            }
        }
    }
}
