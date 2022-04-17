using GRandomizer.Util;
using HarmonyLib;
using Oculus.Newtonsoft.Json.Linq;
using SMLHelper.V2.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class CreatureRandomizer
    {
        static readonly InitializeOnAccess<WeightedSet<TechType>> _weightedCreaturesSet = new InitializeOnAccess<WeightedSet<TechType>>(() =>
        {
            JObject jObject = ConfigReader.ReadFromFile<JObject>("Configs/CreatureRandomizer::CreatureWeights");
            Dictionary<TechType, float> creatureWeights = jObject.ToDictionary<TechType, float>("Configs/CreatureRandomizer.json CreatureWeights", (string str, out TechType techType) => TechTypeExtensions.FromString(str, out techType, true));

            return new WeightedSet<TechType>((from weightKvp in creatureWeights
                                              select new WeightedSet<TechType>.WeightedItem(weightKvp.Key, weightKvp.Value)).ToArray());
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
                if (IsEnabled() && __instance.GetComponent<RandomizedCreature>() == null)
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
