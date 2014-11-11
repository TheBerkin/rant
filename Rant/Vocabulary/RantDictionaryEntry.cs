using System.Collections.Generic;
using System.Linq;

namespace Rant.Vocabulary
{
    /// <summary>
    /// Stores information about a dictionary entry.
    /// </summary>
    public sealed class RantDictionaryEntry
    {
        private RantDictionaryTerm[] _terms;
        private HashSet<string> _classes;
        private int _weight;
        private readonly bool _nsfw;

        /// <summary>
        /// Creates a new Word object from the specified data.
        /// </summary>
        /// <param name="terms">The terms in the entry.</param>
        /// <param name="classes">The classes associated with the entry.</param>
        /// <param name="weight">The weight of the entry.</param>
        public RantDictionaryEntry(string[] terms, IEnumerable<string> classes, bool nsfw = false, int weight = 1)
        {
            _terms = terms.Select(s => new RantDictionaryTerm(s)).ToArray();
            _classes = new HashSet<string>(classes);
            _weight = weight;
            _nsfw = nsfw;
        }

        /// <summary>
        /// Creates a new Word object from the specified data.
        /// </summary>
        /// <param name="terms">The terms in the entry.</param>
        /// <param name="classes">The classes associated with the entry.</param>
        /// <param name="weight">The weight of the entry.</param>
        public RantDictionaryEntry(RantDictionaryTerm[] terms, IEnumerable<string> classes, bool nsfw = false, int weight = 1)
        {
            _terms = terms;
            _classes = new HashSet<string>(classes);
            _weight = weight;
            _nsfw = nsfw;
        }

        /// <summary>
        /// The terms in the entry.
        /// </summary>
        public RantDictionaryTerm[] Terms
        {
            get { return _terms; }
            set { _terms = value ?? new RantDictionaryTerm[0]; }
        }

        /// <summary>
        /// The classes associated with the entry.
        /// </summary>
        public HashSet<string> Classes
        {
            get { return _classes; }
            set { _classes = value ?? new HashSet<string>(); }
        }

        /// <summary>
        /// The weight of the entry.
        /// </summary>
        public int Weight
        {
            get { return _weight; }
            set { _weight = value; }
        }

        /// <summary>
        /// Indicates if the entry is marked as "Not Safe For Work."
        /// </summary>
        public bool NSFW
        {
            get { return _nsfw; }
        }
    }
}