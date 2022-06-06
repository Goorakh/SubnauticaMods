using GRandomizer.RandomizerControllers.Callbacks;
using GRandomizer.Util;
using GRandomizer.Util.Serialization;
using HarmonyLib;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    [RandomizerController]
    static class ItemSizeRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomItemSize;
        }

        static void Reset()
        {
            _itemSizes.Clear();
        }

        public static void Serialize(BinaryWriter writer)
        {
            writer.Write(_itemSizes);
        }

        public static void Deserialize(BinaryReader reader, ushort version)
        {
            _itemSizes.SetTo(reader.ReadDictionary<TechType, Vector2int>());
        }

        static readonly InitializeOnAccessDictionary<TechType, int> _maxIngredientCount = new InitializeOnAccessDictionary<TechType, int>(type =>
        {
            return CraftData.techData.Values.Max((ITechData t) => t.GetIngredients().Sum(i => i.techType == type ? i.amount : 0));
        });

        static readonly InitializeOnAccessDictionary<TechType, float> _maxIngredientFraction = new InitializeOnAccessDictionary<TechType, float>(type =>
        {
            return CraftData.techData.Values.Max((ITechData t) =>
            {
                int totalIngredientCount = 0;
                int thisItemIngredientCount = 0;

                foreach (IIngredient ingredient in t.GetIngredients())
                {
                    int amount = ingredient.amount;

                    totalIngredientCount += amount;

                    if (ingredient.techType == type)
                        thisItemIngredientCount += amount;
                }

                return thisItemIngredientCount / (float)totalIngredientCount;
            });
        });

        static readonly InitializeOnAccessDictionary<TechType, Vector2int> _itemSizes = new InitializeOnAccessDictionary<TechType, Vector2int>(type =>
        {
            Vector2int inventorySize;
            if (Inventory.main.Exists() && Inventory.main.container != null)
            {
                ItemsContainer container = Inventory.main.container;
                inventorySize = new Vector2int(container.sizeX, container.sizeY);
            }
            else
            {
                inventorySize = new Vector2int(6, 8);
            }

            int maxIngredientCount = _maxIngredientCount[type];
            float fraction = _maxIngredientFraction[type];
            bool fits(int x, int y)
            {
                if (x > inventorySize.x || y > inventorySize.y)
                {
                    return false;
                }

                if (maxIngredientCount == 0) // Item is not used in any recipe -> Allow any size as long as it fits in the inventory on its own
                {
                    return true;
                }
                else
                {
                    int maxItemsX = inventorySize.x / x;

                    return Mathf.CeilToInt(maxIngredientCount / (float)maxItemsX) * y <= inventorySize.y * fraction;
                }
            }

            if (!fits(1, 1))
            {
                Utils.LogError($"{type} might not fit in inventory in certain recipes! This is BAD (probably)");
                return new Vector2int(1, 1);
            }

            Vector2int itemSize;
            do
            {
                int randomSize()
                {
                    return Mathf.RoundToInt(1f + (Mathf.Pow(UnityEngine.Random.value, 3f) * 3f));
                }

                itemSize = new Vector2int(randomSize(), randomSize());
            } while (!fits(itemSize.x, itemSize.y));

            return itemSize;
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
