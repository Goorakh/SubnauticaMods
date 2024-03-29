﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace GRandomizer.Util
{
    public class WeightedSet<T>
    {
        readonly WeightedItem[] _items;

        public WeightedSet(WeightedItem[] items)
        {
            _items = items;
        }

        public WeightedSet(IEnumerable<T> items, Func<T, float> weightSelector)
        {
            _items = (from item in items
                      select new WeightedItem(item, weightSelector(item))).ToArray();
        }

        public WeightedSet(IEnumerable<KeyValuePair<T, float>> kvps)
        {
            _items = (from kvp in kvps
                      select new WeightedItem(kvp.Key, kvp.Value)).ToArray();
        }

        public T SelectRandom()
        {
            if (_items == null || _items.Length == 0)
                throw new Exception("No items in weighted set");

            float randomWeight = UnityEngine.Random.Range(0f, _items.Sum(wi => wi.Weight));

            float totalWeight = 0f;
            foreach (WeightedItem weighted in _items)
            {
                totalWeight += weighted.Weight;

                if (totalWeight >= randomWeight)
                    return weighted.Value;
            }

            throw new Exception("Somehow got here, this should not be possible");
        }

        public struct WeightedItem
        {
            public T Value;
            public float Weight;

            public WeightedItem(T value, float weight)
            {
                Value = value;
                Weight = weight;
            }
        }
    }
}
