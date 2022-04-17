﻿using System;
using System.Collections;
using System.Collections.Generic;

namespace GRandomizer.Util
{
    public class InitializeOnAccessDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        public delegate bool ValueSelectorTryGet(TKey key, out TValue value);
        public delegate TValue ValueSelectorGet(TKey key);

        readonly ValueSelectorTryGet _valueSelectorTryGet;
        readonly ValueSelectorGet _valueSelectorGet;

        readonly Dictionary<TKey, TValue> _internalDict;

        public InitializeOnAccessDictionary(ValueSelectorTryGet selector)
        {
            _valueSelectorTryGet = selector;
            _internalDict = new Dictionary<TKey, TValue>();
        }

        public InitializeOnAccessDictionary(ValueSelectorGet selector)
        {
            _valueSelectorGet = selector;
            _internalDict = new Dictionary<TKey, TValue>();
        }

        public TValue this[TKey key]
        {
            get
            {
                if (TryGetValue(key, out TValue value))
                    return value;

                throw new KeyNotFoundException($"Key {key} is not in the dictionary and no value was decided");
            }
            set
            {
                _internalDict[key] = value;
            }
        }

        public ICollection<TKey> Keys => _internalDict.Keys;
        public ICollection<TValue> Values => _internalDict.Values;
        public int Count => _internalDict.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value)
        {
            _internalDict.Add(key, value);
        }

        public void Clear()
        {
            _internalDict.Clear();
        }

        public bool ContainsKey(TKey key)
        {
            return _internalDict.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _internalDict.GetEnumerator();
        }

        public bool Remove(TKey key)
        {
            return _internalDict.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_internalDict.TryGetValue(key, out value))
                return true;

            if (_valueSelectorTryGet != null && _valueSelectorTryGet(key, out value))
            {
                _internalDict[key] = value;
                return true;
            }
            else if (_valueSelectorGet != null)
            {
                value = _internalDict[key] = _valueSelectorGet(key);
                return true;
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }
    }
}
