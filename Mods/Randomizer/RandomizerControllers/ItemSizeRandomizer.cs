using GRandomizer.Util;
using HarmonyLib;
using System.Collections.Generic;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class ItemSizeRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomItemSize;
        }

        static readonly InitializeOnAccessDictionary<TechType, int> _recipeReferenceCount = new InitializeOnAccessDictionary<TechType, int>(type =>
        {
            int refCount = 0;

            foreach (KeyValuePair<TechType, CraftData.TechData> data in CraftData.techData)
            {
                if (data.Key == type)
                {
                    refCount++;
                }
                else
                {
                    if (data.Value._linkedItems != null)
                    {
                        foreach (TechType linked in data.Value._linkedItems)
                        {
                            if (linked == type)
                                refCount++;
                        }
                    }

                    if (data.Value._ingredients != null)
                    {
                        foreach (CraftData.Ingredient ingredient in data.Value._ingredients)
                        {
                            if (ingredient.techType == type)
                                refCount += ingredient.amount;
                        }
                    }
                }
            }

            return refCount;
        });

        static readonly InitializeOnAccessDictionary<TechType, Vector2int> _itemSizes = new InitializeOnAccessDictionary<TechType, Vector2int>(type =>
        {
            int randomSize()
            {
                int refCount = _recipeReferenceCount[type];
                return Mathf.FloorToInt(((refCount > 200 ? 0f : (refCount > 100 ? 1f : (refCount > 50 ? 2f : 3f))) * Mathf.Pow(UnityEngine.Random.value, 3f)) + 1f);
            }

            return new Vector2int(randomSize(), randomSize());
        });

        [HarmonyPatch(typeof(CraftData), nameof(CraftData.GetItemSize))]
        static class CraftData_GetItemSize_Prefix
        {
            static bool Prefix(ref Vector2int __result, TechType techType)
            {
                if (IsEnabled())
                {
                    __result = _itemSizes[techType];
                    return false;
                }

                return true;
            }
        }
    }
}
