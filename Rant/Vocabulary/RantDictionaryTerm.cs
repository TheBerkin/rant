using System;

namespace Rant.Vocabulary
{
    /// <summary>
    /// Represents a Rant dictionary term. A dictionary entry will contain one term for every subtype.
    /// </summary>
    public sealed class RantDictionaryTerm
    {
        private string _value;
        private string _pronunciation;
        private string[] _pronParts;
        private string[] _syllables;
        private int _syllableCount;

        /// <summary>
        /// Creates a new dictionary term with the specified value.
        /// </summary>
        /// <param name="value">The value of the term.</param>
        public RantDictionaryTerm(string value)
        {
            Value = value;
            Pronunciation = "";
        }

        /// <summary>
        /// Creates a new dictionary term with the specified value and pronunciation.
        /// </summary>
        /// <param name="value">The value of the term.</param>
        /// <param name="pronunciation">The pronunciation of the term.</param>
        public RantDictionaryTerm(string value, string pronunciation)
        {
            Value = value;
            Pronunciation = pronunciation ?? "";
        }

        /// <summary>
        /// The value of the term.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set { _value = value; }
        }

        /// <summary>
        /// The pronunciation of the term.
        /// </summary>
        public string Pronunciation
        {
            get { return _pronunciation; }
            set
            {
                _pronunciation = value ?? "";
                if (_pronParts != null) CreatePronParts();
                if (_syllables != null) CreateSyllables();
            }
        }

        /// <summary>
        /// An array containing the individual elements of the pronunciation string. Used by the rhyming system.
        /// </summary>
        public string[] PronunciationParts
        {
            get { return _pronParts ?? CreatePronParts(); }
        }

        /// <summary>
        /// An array containing the individual syllables of the pronunciation string.
        /// </summary>
        public string[] Syllables
        {
            get { return _syllables ?? CreateSyllables(); }
        }

        /// <summary>
        /// The number of syllables in the pronunciation string.
        /// </summary>
        public int SyllableCount
        {
            get
            {
                if (_syllables == null) CreateSyllables();
                return _syllableCount;
            }
        }

        private string[] CreatePronParts()
        {
            return _pronParts = _pronunciation.Split(new[] {' ', '-'}, StringSplitOptions.RemoveEmptyEntries);
        }

        private string[] CreateSyllables()
        {
            _syllables = _pronunciation.Split(new[] {'-'}, StringSplitOptions.RemoveEmptyEntries);
            _syllableCount = _syllables.Length;
            return _syllables;
        }
    }
}