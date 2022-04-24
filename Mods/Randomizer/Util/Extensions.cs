using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.Util
{
    static class Extensions
    {
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
                    Utils.DebugLog($"[RemoveAllComponentsNotIn] Remove component {comp.GetType().FullName} from {comp.name} ({obj.name})", false);
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

        public static void PrepareStaticItem(this GameObject obj)
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
                        Utils.LogError($"[{identifier}] JSON Parse error: Duplicate key {property.Name} ({key})", true);
                    }
                    else
                    {
                        dictionary.Add(key, property.Value.ToObject<TValue>());
                    }
                }
                else
                {
                    Utils.LogWarning($"[{identifier}] Unable to select key for property {property.Name} (missing mod?)", false);
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
    }
}
