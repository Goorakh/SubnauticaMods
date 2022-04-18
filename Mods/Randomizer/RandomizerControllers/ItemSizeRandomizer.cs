using GRandomizer.Util;
using HarmonyLib;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class ItemSizeRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomItemSize;
        }

        static readonly InitializeOnAccessDictionary<TechType, Vector2int> _itemSizes = new InitializeOnAccessDictionary<TechType, Vector2int>(type =>
        {
            int randomSize()
            {
                return Mathf.FloorToInt((3f * Mathf.Pow(UnityEngine.Random.value, 3f)) + 1f);
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
