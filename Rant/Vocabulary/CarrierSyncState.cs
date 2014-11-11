using System;
using System.Collections.Generic;
using System.Linq;

namespace Rant.Vocabulary
{
    /// <summary>
    /// Maintains state information necessary for query synchronization (e.g. rhyming, uniqueness, matching).
    /// </summary>
    public sealed class CarrierSyncState
    {
        /// <summary>
        /// Wherein the KEY is the UNIQUE ID, and the VALUE is the collection of used entries.
        /// </summary>
        private readonly Dictionary<string, HashSet<RantDictionaryEntry>> _uniqueTable = new Dictionary<string, HashSet<RantDictionaryEntry>>();

        /// <summary>
        /// Wherein the KEY is the RHYME ID and SUBTYPE, and the VALUE is the ROOT TERM and USED TERMS.
        /// </summary>
        private readonly Dictionary<string, Tuple<RantDictionaryTerm, HashSet<RantDictionaryEntry>>> _rhymeTable = new Dictionary<string, Tuple<RantDictionaryTerm, HashSet<RantDictionaryEntry>>>(); 

        public CarrierSyncState()
        {
            
        }

        internal RantDictionaryEntry GetUniqueEntry(string uniqueId, IEnumerable<RantDictionaryEntry> entryPool, RNG rng)
        {
            HashSet<RantDictionaryEntry> usedSet;
            if (!_uniqueTable.TryGetValue(uniqueId, out usedSet))
            {
                usedSet = _uniqueTable[uniqueId] = new HashSet<RantDictionaryEntry>();
            }
            var selection = entryPool.Except(usedSet).PickWeighted(rng, e => e.Weight);
            
            if (selection != null) usedSet.Add(selection);

            return selection;
        }

        internal RantDictionaryEntry GetRhymingEntry(string rhymeId, int subtype,
            IEnumerable<RantDictionaryEntry> entryPool, RNG rng)
        {
            Tuple<RantDictionaryTerm, HashSet<RantDictionaryEntry>> rhymeState;
            if (!_rhymeTable.TryGetValue(rhymeId, out rhymeState))
            {
                var entry = entryPool
                    .Where(e => !String.IsNullOrWhiteSpace(e.Terms[subtype].Pronunciation))
                    .PickWeighted(rng, e => e.Weight);
                _rhymeTable[rhymeId] = Tuple.Create(entry.Terms[subtype], new HashSet<RantDictionaryEntry>(new[] { entry }));
                return entry;
            }
            var selection =
                entryPool.Except(rhymeState.Item2)
                    .Where(e => !String.IsNullOrWhiteSpace(e.Terms[subtype].Pronunciation))
                            .PickWeighted(rng, e => e.Weight * VocabUtils.RhymeIndex(rhymeState.Item1, e.Terms[subtype]));

            if (selection != null) rhymeState.Item2.Add(selection);
            return selection;
        }
    }
}