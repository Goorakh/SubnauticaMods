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

        public static void SetRigidbodiesKinematic(this GameObject obj, bool kinematic)
        {
            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = kinematic;
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
        public static void DisableAllCollidersOfType<T>(this GameObject obj, ColliderFlags flags = ColliderFlags.NonTrigger) where T : Collider
        {
            foreach (T collider in obj.GetComponentsInChildren<T>())
            {
                if (((flags & ColliderFlags.NonTrigger) != 0 && !collider.isTrigger) || ((flags & ColliderFlags.Trigger) != 0 && collider.isTrigger))
                {
                    collider.enabled = false;
                }
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
                if (cone.Exists())
                    cone.gameObject.SetActive(false);
            }

            obj.SetRigidbodiesKinematic(true);
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

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this JObject jObject, string identifier, TryConvert<JProperty, TKey> keySelector)
        {
            if (jObject is null)
                throw new ArgumentNullException(nameof(jObject));

            if (keySelector is null)
                throw new ArgumentNullException(nameof(keySelector));

            Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

            foreach (JToken token in jObject.ChildrenTokens)
            {
                if (token is JProperty property)
                {
                    if (keySelector(property, out TKey key))
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
                else
                {
                    Utils.LogWarning($"[{identifier}] Child token {token.Path} is {token.Type} ({token.GetType().FullName}), expected {nameof(JProperty)}");
                }
            }

            return dictionary;
        }
        
        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> valuePairs)
        {
            return valuePairs.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
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

        public static int FindArgumentIndex(this MethodBase mb, Predicate<ParameterInfo> parameterPredicate)
        {
            int index = Array.FindIndex(mb.GetParameters(), parameterPredicate);
            return index + (index != -1 && !mb.IsStatic ? 1 : 0);
        }
        public static int FindArgumentIndex(this MethodBase mb, Type parameterType)
        {
            return mb.FindArgumentIndex(p => p.ParameterType == parameterType);
        }

        public static Dictionary<T, T> ToRandomizedReplacementDictionary<T>(this IEnumerable<T> enumerable)
        {
            Dictionary<T, T> result = new Dictionary<T, T>();

            List<T> itemsList = enumerable.ToList();

            T[] keys = new T[itemsList.Count];
            itemsList.CopyTo(keys, 0);

            foreach (T key in keys)
            {
                result.Add(key, itemsList.GetAndRemoveRandom());
            }

            return result;
        }

        public static MethodInfo FindMethod(this IEnumerable<MethodInfo> methods, Type returnType, Type[] parameters)
        {
            return methods.Single(mi => mi.ReturnType == returnType && mi.GetParameters().Select(m => m.ParameterType).SequenceEqual(parameters));
        }

        public static void CopyValuesFrom<T>(this T dest, T source, Predicate<FieldInfo> fieldPredicate = null)
        {
            foreach (FieldInfo field in AccessTools.GetDeclaredFields(typeof(T)))
            {
                if (field.IsStatic || field.IsLiteral)
                    continue;

                if (fieldPredicate == null || fieldPredicate(field))
                {
                    field.SetValue(dest, field.GetValue(source));
                }
            }
        }

        public static T AddComponentCopy<T>(this GameObject obj, T source) where T : Component
        {
            T newComp = obj.AddComponent<T>();

            newComp.CopyValuesFrom(source, fi => ((fi.IsPublic && fi.GetCustomAttribute<NonSerializedAttribute>() == null) || fi.GetCustomAttribute<SerializeField>() != null) && (!typeof(Behaviour).IsAssignableFrom(fi.FieldType) || fi.Name.ToLower().Contains("prefab")));

            return newComp;
        }

        public static T AddComponentChildCopy<T>(this Transform parent, string childName, T source) where T : Component
        {
            GameObject newObj = new GameObject(childName);
            newObj.SetActive(false);
            newObj.transform.SetParent(parent);

            T newComp = newObj.AddComponentCopy(source);
            newObj.SetActive(true);

            return newComp;
        }

        public static InventoryItem Add(this Equipment equipment, EquipmentType slotType, TechType itemType, string preferredSlotID = null)
        {
            string slot = preferredSlotID;
            if ((slot != null && Equipment.GetSlotType(slot) == slotType && equipment.GetTechTypeInSlot(slot) == TechType.None) || equipment.GetFreeSlot(slotType, out slot))
            {
                Pickupable pickupable = CraftData.InstantiateFromPrefab(itemType).GetComponent<Pickupable>().Pickup(false);
                InventoryItem item = new InventoryItem(pickupable);
                if (!equipment.AddItem(slot, item, true))
                {
                    Utils.LogWarning($"Unable to add {itemType} in slot '{slot}'");

                    if (pickupable.Exists())
                    {
                        GameObject.Destroy(pickupable.gameObject);
                    }

                    return null;
                }

                return item;
            }
            else
            {
                Utils.LogWarning($"Equipment has no free slot of type {slotType} (preferred: {preferredSlotID ?? "null"})");
            }

            return null;
        }

        public static bool TryFindSeamothStorageForSlotID(this SeaMoth seamoth, string storageModuleSlotID, out ItemsContainer container)
        {
            int slotIndex = Array.IndexOf(SeaMoth._slotIDs, storageModuleSlotID);
            if (slotIndex != -1)
            {
                container = seamoth.GetStorageInSlot(slotIndex, TechType.VehicleStorageModule);
                if (container == null)
                {
                    Utils.LogWarning($"Could not find storage container for index {slotIndex}");
                    return false;
                }

                return true;
            }
            else
            {
                Utils.LogWarning($"Could not find seamoth slot index for '{storageModuleSlotID}'");
            }

            container = null;
            return false;
        }

        public static void TryDisableChild(this Transform root, string path)
        {
            Transform child = root.Find(path);
            if (child.Exists())
                child.gameObject.SetActive(false);
        }
    }
}
