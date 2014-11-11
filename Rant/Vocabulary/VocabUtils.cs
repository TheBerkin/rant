using System;
using System.Collections.Generic;
using System.Linq;

namespace Rant.Vocabulary
{
    internal static class VocabUtils
    {
        public static T PickWeighted<T>(this IEnumerable<T> collection, RNG rng, Func<T, int> weightSelectionFunc, T defaultValue = default(T))
        {
            int selection = rng.Next(collection.Sum(weightSelectionFunc));

            foreach (T t in collection)
            {
                if (selection < weightSelectionFunc(t))
                {
                    return t;
                }
                selection -= weightSelectionFunc(t);
            }
            return defaultValue;
        }

        public static T PickWeighted<T>(this IEnumerable<T> collection, RNG rng, Func<T, int> weightSelectionFunc, Func<RNG, int, int> rngSelectionFunc, T defaultValue = default(T))
        {
            int selection = rngSelectionFunc(rng, collection.Sum(weightSelectionFunc));

            foreach (T t in collection)
            {
                if (selection < weightSelectionFunc(t))
                {
                    return t;
                }
                selection -= weightSelectionFunc(t);
            }
            return defaultValue;
        }

        public static int RhymeIndex(RantDictionaryTerm baseValue, RantDictionaryTerm testValue)
        {
            if (baseValue == null || testValue == null) return 0;
            var baseParts = baseValue.PronunciationParts;
            var testParts = testValue.PronunciationParts;
            int index = 0;
            int len = Math.Min(baseParts.Length, testParts.Length);
            for (int i = 0; i < len; i++)
            {
                if (baseParts[baseParts.Length - (i + 1)] == testParts[testParts.Length - (i + 1)])
                {
                    index++;
                }
                else
                {
                    return index;
                }
            }
            return index;
        }
    }
}