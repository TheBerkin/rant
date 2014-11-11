using System;
using System.Collections.Generic;
using System.Linq;

namespace Rant.Vocabulary
{
    /// <summary>
    /// Stores a Rant dictionary.
    /// </summary>
    public sealed partial class RantDictionary
    {
        private readonly string _name;
        private readonly string[] _subtypes;
        private readonly RantDictionaryEntry[] _words;

        /// <summary>
        /// Creates a new WordList from the specified data.
        /// </summary>
        /// <param name="name">the name of the list.</param>
        /// <param name="subtypes">The subtype names.</param>
        /// <param name="words">The words to add to the list.</param>
        public RantDictionary(string name, string[] subtypes, IEnumerable<RantDictionaryEntry> words)
        {
            if (!Util.ValidateName(name))
            {
                throw new FormatException("Invalid dictionary name: '" + name + "'");
            }

            if (!subtypes.All(Util.ValidateName))
            {
                throw new FormatException("Invalid subtype name(s): " + 
                    subtypes.Where(s => !Util.ValidateName(s))
                    .Select(s => String.Concat("'", s, "'"))
                    .Aggregate((c,n) => c + ", " + n));
            }

            _subtypes = subtypes;
            _name = name;
            _words = words.ToArray();
        }
        
        /// <summary>
        /// The entries stored in the dictionary.
        /// </summary>
        public RantDictionaryEntry[] Entries
        {
            get { return _words; }
        }

        /// <summary>
        /// The subtypes in the dictionary.
        /// </summary>
        public string[] Subtypes
        {
            get { return _subtypes; }
        }

        /// <summary>
        /// The name of the dictionary.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        private int GetSubtypeIndex(string subtype)
        {
            if (String.IsNullOrEmpty(subtype)) return 0;
            for (int i = 0; i < _subtypes.Length; i++)
            {
                if (String.Equals(subtype, _subtypes[i], StringComparison.OrdinalIgnoreCase)) return i;
            }
            return -1;
        }

        internal string Query(RNG rng, Query query, CarrierSyncState syncState)
        {
            var index = String.IsNullOrEmpty(query.Subtype) ? 0 : GetSubtypeIndex(query.Subtype);
            if (index == -1)
            {
                return "[Missing Subtype]";
            }

            IEnumerable<RantDictionaryEntry> pool = _words;

            pool = query.Exclusive
                ? pool.Where(e => e.Classes.Any() && e.Classes.All(
                    c => query.ClassFilters.Any(
                        set => set.Any(t => t.Item2 == c))))
                : pool.Where(e => query.ClassFilters.All(
                    set => set.Any(
                        t => t.Item1 == (e.Classes.Contains(t.Item2)))));

            pool = query.RegexFilters.Aggregate(pool, (current, regex) => current.Where(e => regex.Item1 == regex.Item2.IsMatch(e.Terms[index].Value)));

            if (!pool.Any())
            {
                return "[?]";
            }

            RantDictionaryEntry entry = null;

            if (query.Carrier != null)
            {
                switch (query.Carrier.SyncType)
                {
                    case CarrierSyncType.Match:
                        entry =
                            pool.PickWeighted(rng, e => e.Weight, (r, n) => r.PeekAt(query.Carrier.SyncId.Hash(), n));
                        break;
                    case CarrierSyncType.Unique:
                        entry = syncState.GetUniqueEntry(query.Carrier.SyncId, pool, rng);
                        break;
                    case CarrierSyncType.Rhyme:
                        entry = syncState.GetRhymingEntry(query.Carrier.SyncId, index, pool, rng);
                        break;
                }
            }
            else
            {
                entry = pool.PickWeighted(rng, e => e.Weight);
            }


            return entry == null ? "[?]" : entry.Terms[index].Value;
        }
    }
}