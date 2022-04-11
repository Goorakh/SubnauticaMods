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
    class CreatureRandomizer : AutoSingleton<CreatureRandomizer>, IRandomizerController
    {
        static TechType[] _creatureTypes;
        static TechType[] creatures
        {
            get
            {
                if (_creatureTypes == null)
                {
                    List<TechType> creatureTypesList = new List<TechType>();

                    string[] blacklistStrings = ConfigReader.ReadFromFile<string[]>("Randomizers/CreatureRandomizer::Blacklist");

                    foreach (TechType type in Utils.GetAllDefinedTechTypes())
                    {
                        string typeString = type.AsString(true);
                        if (blacklistStrings.Any(bl => string.Equals(typeString, bl, StringComparison.OrdinalIgnoreCase)))
                            continue;

                        GameObject prefab = CraftData.GetPrefabForTechType(type);
                        if (prefab != null)
                        {
                            Creature creature = prefab.GetComponent<Creature>();
                            if (creature != null)
                            {
                                creatureTypesList.Add(type);
                            }
                        }
                    }

                    _creatureTypes = creatureTypesList.ToArray();
                }

                return _creatureTypes;
            }
        }

        static WeightedSet<TechType> _weightedCreaturesSet;
        static WeightedSet<TechType> weightedCreaturesSet
        {
            get
            {
                if (_weightedCreaturesSet == null)
                {
                    JObject jObject = ConfigReader.ReadFromFile<JObject>("Randomizers/CreatureRandomizer::CreatureWeights");
                    Dictionary<TechType, float> creatureWeights = jObject.ToDictionary<TechType, float>("Randomizers/CreatureRandomizer.json CreatureWeights", (string str, out TechType techType) => TechTypeExtensions.FromString(str, out techType, true));
                    
                    _weightedCreaturesSet = new WeightedSet<TechType>((from weightKvp in creatureWeights
                                                                       select new WeightedSet<TechType>.WeightedItem(weightKvp.Key, weightKvp.Value)).ToArray());
                }

                return _weightedCreaturesSet;
            }
        }

        public bool IsEnabled()
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
                if (Instance.IsEnabled() && __instance.GetComponent<RandomizedCreature>() == null)
                {
                    TechType oldCreatureType = CraftData.GetTechType(__instance.gameObject);

                    TechType newCreatureType;
                    do
                    {
                        newCreatureType = weightedCreaturesSet.SelectRandom();
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
