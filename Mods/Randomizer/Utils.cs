using GRandomizer.Util;
using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GRandomizer
{
    public static class Utils
    {
        public static int GetStringFormatCount(string str)
        {
            return Regex.Matches(str, @"(?<!\{)(?>\{\{)*\{\d(.*?)").Count;
        }

        public static Vector3 Abs(Vector3 vector3)
        {
            vector3.x = Mathf.Abs(vector3.x);
            vector3.y = Mathf.Abs(vector3.y);
            vector3.z = Mathf.Abs(vector3.z);
            return vector3;
        }

        public static IEnumerable<TechType> GetAllDefinedTechTypes()
        {
            foreach (TechType techType in (TechType[])Enum.GetValues(typeof(TechType)))
            {
                yield return techType;
            }
        }

        public static void LogError(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Error, log, showOnScreen);
        }

        public static void LogInfo(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Info, log, showOnScreen);
        }

        public static void LogWarning(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Warn, log, showOnScreen);
        }

#if DEBUG
        public static void DebugLog(string log, bool showOnScreen = false)
        {
            logLevel(QModManager.Utility.Logger.Level.Debug, log, showOnScreen);
        }
#endif

        static void logLevel(QModManager.Utility.Logger.Level level, string log, bool showOnScreen)
        {
            QModManager.Utility.Logger.Log(level, $"[GRandomizer]: {log}", null, showOnScreen);
        }

        public static void DisableRigidbodies(this GameObject obj)
        {
            foreach (Rigidbody rb in obj.GetComponentsInChildren<Rigidbody>(true))
            {
                rb.isKinematic = true;
            }
        }

        public static void RemoveAllComponentsNotIn(this GameObject obj, GameObject other)
        {
            foreach (MonoBehaviour comp in obj.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (other.GetComponentInChildren(comp.GetType(), true) == null)
                {
#if DEBUG
                    DebugLog($"[RemoveAllComponentsNotIn] Remove component {comp.GetType().FullName} from {comp.name} ({obj.name})", false);
#endif

                    GameObject.Destroy(comp);
                }
            }
        }

        public static bool TryGetModelBounds(this GameObject obj, out Bounds bounds)
        {
            Bounds? modelBounds = null;
            foreach (Renderer rend in obj.GetComponentsInChildren<Renderer>())
            {
                if (rend is ParticleSystemRenderer)
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

        public static void PrepareStaticItem(GameObject obj)
        {
            obj.SetActive(true);

            if (obj.GetComponent<FlashLight>() != null)
            {
                Transform cone = obj.transform.Find("lights_parent/x_flashlightCone");
                if (cone != null)
                    cone.gameObject.SetActive(false);
            }

            obj.DisableRigidbodies();
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
                        LogError($"[{identifier}] JSON Parse error: Duplicate key {property.Name} ({key})", true);
                    }
                    else
                    {
                        dictionary.Add(key, property.Value.ToObject<TValue>());
                    }
                }
                else
                {
                    LogWarning($"[{identifier}] Unable to select key for property {property.Name} (missing mod?)", false);
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

        public static class Random
        {
            public static Color Color(float a = 1f)
            {
                return new Color(UnityEngine.Random.value, UnityEngine.Random.value, UnityEngine.Random.value, a);
            }

            public static Quaternion Rotation => Quaternion.Euler(UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f), UnityEngine.Random.Range(-180f, 180f));
        }
    }
}
