using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public class ReplacementDictionary<T> : IDictionary<T, T>
    {
        DualDictionary<T, T> _replacements;

        ICollection<T> IDictionary<T, T>.Keys { get; }
        ICollection<T> IDictionary<T, T>.Values { get; }
        int ICollection<KeyValuePair<T, T>>.Count => _replacements.Count();
        bool ICollection<KeyValuePair<T, T>>.IsReadOnly { get; }

        T IDictionary<T, T>.this[T key]
        {
            get
            {
                return _replacements.F2S_Get(key);
            }
            set
            {
                throw new NotImplementedException();
            }
        }

        public ReplacementDictionary(Dictionary<T, T> replacements) : this(new DualDictionary<T, T>(replacements))
        {
        }

        public ReplacementDictionary(DualDictionary<T, T> replacements)
        {
            _replacements = replacements;
        }

        public T GetReplacement(T original)
        {
            return _replacements.F2S_Get(original);
        }
        public bool TryGetReplacement(T original, out T replacement)
        {
            return _replacements.F2S_TryGetValue(original, out replacement);
        }
        public bool HasReplacementFor(T original)
        {
            return _replacements.F2S_ContainsKey(original);
        }

        public T GetOriginal(T replacement)
        {
            return _replacements.S2F_Get(replacement);
        }
        public bool TryGetOriginal(T replacement, out T original)
        {
            return _replacements.S2F_TryGetValue(replacement, out original);
        }
        public bool HasOriginalFor(T replacement)
        {
            return _replacements.S2F_ContainsKey(replacement);
        }

        public void Set(Dictionary<T, T> replacements)
        {
            Set(new DualDictionary<T, T>(replacements));
        }
        public void Set(DualDictionary<T, T> replacements)
        {
            _replacements = replacements;
        }

        bool IDictionary<T, T>.ContainsKey(T key)
        {
            throw new NotImplementedException();
        }

        void IDictionary<T, T>.Add(T key, T value)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<T, T>.Remove(T key)
        {
            throw new NotImplementedException();
        }

        bool IDictionary<T, T>.TryGetValue(T key, out T value)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<T, T>>.Add(KeyValuePair<T, T> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<T, T>>.Clear()
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<T, T>>.Contains(KeyValuePair<T, T> item)
        {
            throw new NotImplementedException();
        }

        void ICollection<KeyValuePair<T, T>>.CopyTo(KeyValuePair<T, T>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        bool ICollection<KeyValuePair<T, T>>.Remove(KeyValuePair<T, T> item)
        {
            throw new NotImplementedException();
        }

        IEnumerator<KeyValuePair<T, T>> IEnumerable<KeyValuePair<T, T>>.GetEnumerator()
        {
            return _replacements.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _replacements.GetEnumerator();
        }

        public static implicit operator DualDictionary<T, T>(ReplacementDictionary<T> replacementDict)
        {
            return replacementDict._replacements;
        }
    }
}
