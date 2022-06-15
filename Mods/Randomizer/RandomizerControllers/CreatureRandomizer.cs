using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using HarmonyLib;
using Oculus.Newtonsoft.Json.Linq;
using System.Linq;
using UnityEngine;
using UnityModdingUtility;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class CreatureRandomizer
    {
        static readonly InitializeOnAccess<WeightedSet<TechType>> _weightedCreaturesSet = new InitializeOnAccess<WeightedSet<TechType>>(() =>
        {
            JObject jObject = ConfigReader.ReadFromFile<JObject>("Configs/CreatureRandomizer::CreatureWeights");

            return new WeightedSet<TechType>(jObject.ToDictionary<TechType, float>("Configs/CreatureRandomizer.json CreatureWeights", (JProperty prop, out TechType techType) => TechTypeExtensions.FromString(prop.Name, out techType, true)));
        });

        static bool IsEnabled()
        {
            return Mod.Config.RandomCreatures;
        }

        class RandomizedCreature : MonoBehaviour
        {
        }

        [HarmonyPatch(typeof(Creature), "Start")]
        static class Creature_Start_Patch
        {
            static void Prefix(Creature __instance)
            {
                if (IsEnabled() && !__instance.HasComponent<RandomizedCreature>())
                {
                    TechType oldCreatureType = CraftData.GetTechType(__instance.gameObject);

                    TechType newCreatureType;
                    do
                    {
                        newCreatureType = _weightedCreaturesSet.Get.SelectRandom();
                    } while (newCreatureType == oldCreatureType);

                    GameObject newCreatureObj = CraftData.InstantiateFromPrefab(newCreatureType);

                    Transform newCreatureTransform = newCreatureObj.transform;
                    Transform oldCreatureTransform = __instance.transform;

                    newCreatureTransform.SetParent(oldCreatureTransform.parent, false);
                    newCreatureTransform.localPosition = oldCreatureTransform.localPosition;
                    newCreatureTransform.localRotation = oldCreatureTransform.localRotation;
                    newCreatureTransform.localScale = oldCreatureTransform.localScale;

                    newCreatureObj.AddComponent<RandomizedCreature>();
                    GameObject.Destroy(__instance.gameObject);
                }
            }
        }
    }
}
