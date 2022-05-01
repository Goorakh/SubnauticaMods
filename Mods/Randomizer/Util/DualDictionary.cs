using Oculus.Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    class DualDictionary<TFirst, TSecond> : IEnumerable<KeyValuePair<TFirst, TSecond>>
    {
        readonly Dictionary<TFirst, TSecond> _firstToSecond;
        readonly Dictionary<TSecond, TFirst> _secondToFirst;

        public DualDictionary() : this(Enumerable.Empty<KeyValuePair<TFirst, TSecond>>())
        {
        }

        public DualDictionary(IEnumerable<KeyValuePair<TFirst, TSecond>> collection)
        {
            _firstToSecond = new Dictionary<TFirst, TSecond>();
            _secondToFirst = new Dictionary<TSecond, TFirst>();

            foreach (KeyValuePair<TFirst, TSecond> pair in collection)
            {
                _firstToSecond.Add(pair.Key, pair.Value);
                _secondToFirst.Add(pair.Value, pair.Key);
            }
        }

        public IEnumerator<KeyValuePair<TFirst, TSecond>> GetEnumerator()
        {
            return _firstToSecond.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add((TFirst, TSecond) valueTuple)
        {
            Add(valueTuple.ToTuple());
        }

        public void Add(Tuple<TFirst, TSecond> valueTuple)
        {
            _firstToSecond.Add(valueTuple.Item1, valueTuple.Item2);
            _secondToFirst.Add(valueTuple.Item2, valueTuple.Item1);
        }

        public TSecond F2S_Get(TFirst key)
        {
            return _firstToSecond[key];
        }
        public TFirst S2F_Get(TSecond key)
        {
            return _secondToFirst[key];
        }

        public bool F2S_TryGetValue(TFirst key, out TSecond value)
        {
            return _firstToSecond.TryGetValue(key, out value);
        }

        public bool S2F_TryGetValue(TSecond key, out TFirst value)
        {
            return _secondToFirst.TryGetValue(key, out value);
        }
    }
}
