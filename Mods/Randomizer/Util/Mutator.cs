using System.Collections.Generic;

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
