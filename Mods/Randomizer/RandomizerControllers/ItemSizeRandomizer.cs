using GRandomizer.Util;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GRandomizer.RandomizerControllers
{
    static class ItemSizeRandomizer
    {
        static bool IsEnabled()
        {
            return Mod.Config.RandomItemSize;
        }

        static readonly Dictionary<TechType, Vector2int> _itemSizes = new Dictionary<TechType, Vector2int>();
        static Vector2int getOverrideItemSize(TechType type)
        {
            if (_itemSizes.TryGetValue(type, out Vector2int size))
            {
                return size;
            }
            else
            {
                int randomSize()
                {
                    return Mathf.FloorToInt((3f * Mathf.Pow(UnityEngine.Random.value, 3f)) + 1f);
                }

                return _itemSizes[type] = new Vector2int(randomSize(), randomSize());
            }
        }

        [HarmonyPatch(typeof(CraftData), nameof(CraftData.GetItemSize))]
        static class CraftData_GetItemSize_Prefix
        {
            static bool Prefix(ref Vector2int __result, TechType techType)
            {
                if (IsEnabled())
                {
                    __result = getOverrideItemSize(techType);
                    return false;
                }

                return true;
            }
        }
    }
}
