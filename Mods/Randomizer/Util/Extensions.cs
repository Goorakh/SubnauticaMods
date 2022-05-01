using HarmonyLib;
using Oculus.Newtonsoft.Json;
using Oculus.Newtonsoft.Json.Linq;
using QModManager.API;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    static class Extensions
    {
        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool Exists(this UnityEngine.Object obj)
        {
            return obj && obj != null;
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponent<T>(this Component component) where T : UnityEngine.Object
        {
            return component.GetComponent<T>().Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponent(this Component component, Type componentType)
        {
            return component.GetComponent(componentType).Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponent<T>(this GameObject obj) where T : UnityEngine.Object
        {
            return obj.GetComponent<T>().Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponent(this GameObject obj, Type componentType)
        {
            return obj.GetComponent(componentType).Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponentInChildren<T>(this Component component, bool includeInactive = false) where T : UnityEngine.Object
        {
            return component.GetComponentInChildren<T>(includeInactive).Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponentInChildren(this Component component, Type componentType, bool includeInactive = false)
        {
            return component.GetComponentInChildren(componentType, includeInactive).Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponentInChildren<T>(this GameObject obj, bool includeInactive = false) where T : UnityEngine.Object
        {
            return obj.GetComponentInChildren<T>(includeInactive).Exists();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool HasComponentInChildren(this GameObject obj, Type componentType, bool includeInactive = false)
        {
            return obj.GetComponentInChildren(componentType, includeInactive).Exists();
        }

        public static void DisableRigidbodies(this GameObject obj)
        {
            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
            }
        }

        public static void DisableAllComponentsOfType<T>(this GameObject obj) where T : Behaviour
        {
            foreach (T collider in obj.GetComponentsInChildren<T>())
            {
                collider.enabled = false;
            }
        }

        // FU Unity
        public static void DisableAllCollidersOfType<T>(this GameObject obj) where T : Collider
        {
            foreach (T collider in obj.GetComponentsInChildren<T>())
            {
                collider.enabled = false;
            }
        }

        public static void RemoveAllComponentsNotIn(this GameObject obj, GameObject other)
        {
            // Just disabling the components for now, destroying leads to a seemingly endless pit of problems related to RequireComponent, and I am so sick of dealing with that shit.
            // If this breaks something: Too bad! :)
            foreach (MonoBehaviour comp in obj.GetComponentsInChildren<MonoBehaviour>())
            {
                Type compType = comp.GetType();
                if (!other.HasComponentInChildren(compType))
                {
#if VERBOSE
                    Utils.DebugLog($"Disable component {compType.FullName} ({comp.name})", false);
#endif
                    comp.enabled = false;
                    //GameObject.DestroyImmediate(comp);
                }
            }
        }

        public static bool TryGetModelBounds(this GameObject obj, out Bounds bounds)
        {
            Bounds? modelBounds = null;
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (rend is ParticleSystemRenderer || !rend.enabled)
                    continue;

                if (modelBounds == null)
                {
                    modelBounds = rend.bounds;
                }
                else
                {
                    Bounds value = modelBounds.Value;
                    value.Encapsulate(rend.bounds);
                    modelBounds = value;
                }
            }

            if (modelBounds.HasValue)
            {
                bounds = modelBounds.Value;
                return true;
            }
            else
            {
                bounds = default;
                return false;
            }
        }

        public static void PrepareStaticItem(this GameObject obj)
        {
            obj.SetActive(true);

            if (obj.HasComponent<FlashLight>())
            {
                Transform cone = obj.transform.Find("lights_parent/x_flashlightCone");
                if (cone != null)
                    cone.gameObject.SetActive(false);
            }

            obj.DisableRigidbodies();
        }

        public static void AddVFXFabricatingComponentIfMissing(this GameObject obj, bool checkChildrenForExistingComponent)
        {
            bool hasComponent;
            if (checkChildrenForExistingComponent)
            {
                hasComponent = obj.HasComponentInChildren<VFXFabricating>();
            }
            else
            {
                hasComponent = obj.HasComponent<VFXFabricating>();
            }

            if (!hasComponent)
            {
                VFXFabricating fabricating = obj.AddComponent<VFXFabricating>();

                if (obj.TryGetModelBounds(out Bounds modelBounds))
                {
                    Vector3 center = modelBounds.center;

                    Vector3 halfHeight = new Vector3(0f, modelBounds.size.y / 2f, 0f);
                    Vector3 bottomCenter = center - halfHeight;
                    Vector3 topCenter = center + halfHeight;

                    fabricating.localMinY = fabricating.transform.InverseTransformPoint(bottomCenter).y;
                    fabricating.localMaxY = fabricating.transform.InverseTransformPoint(topCenter).y;

                    fabricating.posOffset = fabricating.transform.InverseTransformPoint(fabricating.transform.position - bottomCenter);
                }
                else
                {
                    fabricating.localMinY = 0f;
                    fabricating.localMaxY = 1f;

#if VERBOSE
                    Utils.DebugLog($"No bounds for {obj.name}, using fallback parameters");
#endif
                }
            }
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this JObject jObject, string identifier, TryConvert<string, TKey> keySelector)
        {
            if (jObject is null)
                throw new ArgumentNullException(nameof(jObject));

            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

            foreach (JToken token in jObject.ChildrenTokens)
            {
                JProperty property = (JProperty)token;
                if (keySelector(property.Name, out TKey key))
                {
                    if (dictionary.ContainsKey(key))
                    {
                        Utils.LogError($"[{identifier}] JSON Parse error: Duplicate key {property.Name} ({key})", true, 1);
                    }
                    else
                    {
                        dictionary.Add(key, property.Value.ToObject<TValue>());
                    }
                }
                else
                {
                    Utils.LogWarning($"[{identifier}] Unable to select key for property {property.Name} (missing mod?)", false, 1);
                }
            }

            return dictionary;
        }

        public static T GetAndRemoveRandom<T>(this IList<T> list)
        {
            return list.GetAndRemove(UnityEngine.Random.Range(0, list.Count));
        }

        public static T GetAndRemove<T>(this IList<T> list, int index)
        {
            T result = list[index];
            list.RemoveAt(index);
            return result;
        }

        public static int GetLocalIndex(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldloc_0 || instruction.opcode == OpCodes.Stloc_0)
            {
                return 0;
            }
            else if (instruction.opcode == OpCodes.Ldloc_1 || instruction.opcode == OpCodes.Stloc_1)
            {
                return 1;
            }
            else if (instruction.opcode == OpCodes.Ldloc_2 || instruction.opcode == OpCodes.Stloc_2)
            {
                return 2;
            }
            else if (instruction.opcode == OpCodes.Ldloc_3 || instruction.opcode == OpCodes.Stloc_3)
            {
                return 3;
            }
            else if (instruction.opcode == OpCodes.Ldloc || instruction.opcode == OpCodes.Ldloc_S || instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S ||
                     instruction.opcode == OpCodes.Stloc || instruction.opcode == OpCodes.Stloc_S)
            {
                if (AccessTools.IsInteger(instruction.operand.GetType()))
                {
                    return Convert.ToInt32(instruction.operand);
                }
                else if (instruction.operand is LocalBuilder localBuilder)
                {
                    return localBuilder.LocalIndex;
                }
                else
                {
                    throw new NotImplementedException($"Operand of type {instruction.operand.GetType().FullName} is not implemented");
                }
            }
            else
            {
                throw new ArgumentException($"OpCode did not match Stloc* or Ldloc* OpCodes ({instruction.opcode.Name})", nameof(instruction));
            }
        }

        public static int GetArgumentIndex(this CodeInstruction instruction)
        {
            if (instruction.opcode == OpCodes.Ldarg_0)
            {
                return 0;
            }
            else if (instruction.opcode == OpCodes.Ldarg_1)
            {
                return 1;
            }
            else if (instruction.opcode == OpCodes.Ldarg_2)
            {
                return 2;
            }
            else if (instruction.opcode == OpCodes.Ldarg_3)
            {
                return 3;
            }
            else if (instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S || instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S ||
                     instruction.opcode == OpCodes.Starg || instruction.opcode == OpCodes.Starg_S)
            {
                if (AccessTools.IsInteger(instruction.operand.GetType()))
                {
                    return Convert.ToInt32(instruction.operand);
                }
                else
                {
                    throw new NotImplementedException($"Operand of type {instruction.operand.GetType().FullName} is not implemented");
                }
            }
            else
            {
                throw new ArgumentException($"OpCode did not match Starg* or Ldarg* OpCodes ({instruction.opcode.Name})", nameof(instruction));
            }
        }

        public static bool IsAny(this OpCode op, params OpCode[] opcodes)
        {
            return opcodes.Any(o => op == o);
        }

        public static Type GetMethodReturnType(this MethodBase mb)
        {
            if (mb is MethodInfo mi)
            {
                return mi.ReturnType;
            }
            else if (mb is ConstructorInfo ci)
            {
                return ci.DeclaringType;
            }
            else
            {
                throw new NotImplementedException($"{mb.GetType().FullName} is not implemented");
            }
        }

        public static T GetRandomOrDefault<T>(this IEnumerable<T> collection)
        {
            return collection.ElementAt(UnityEngine.Random.Range(0, collection.Count()));
        }

        public static object GetRandomOrDefault(this IEnumerable collection)
        {
            return collection.Cast<object>().GetRandomOrDefault();
        }

        static HashSet<Assembly> _modAssemblies;
        public static bool IsFromMod(this Assembly assembly)
        {
            if (_modAssemblies == null)
            {
                _modAssemblies = (from mod in QModServices.Main.GetAllMods()
                                  where mod.IsLoaded && mod.LoadedAssembly != null
                                  select mod.LoadedAssembly).ToHashSet();
            }

            return _modAssemblies.Contains(assembly);
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool IsFromModAssembly(this Type type)
        {
            return type.Assembly.IsFromMod();
        }

        [MethodImpl(MethodImplAttributes.AggressiveInlining)]
        public static bool IsFromModAssembly(this MemberInfo member)
        {
            return member.DeclaringType.IsFromModAssembly();
        }
    }
}
