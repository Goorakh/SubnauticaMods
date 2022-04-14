using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GRandomizer.Util
{
    public delegate T Mutator<T>(T original);

    public static class MutatorExtensions
    {
        public static T Mutate<T>(this IEnumerable<Mutator<T>> mutators, T value)
        {
            foreach (Mutator<T> mutator in mutators)
            {
                value = mutator(value);
            }

            return value;
        }
    }
}
