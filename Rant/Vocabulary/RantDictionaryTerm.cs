#region License

// https://github.com/TheBerkin/Rant
// 
// Copyright (c) 2017 Nicholas Fleck
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;

using Rant.Core.Utilities;

using static Rant.Localization.Txtres;

namespace Rant.Vocabulary
{
    /// <summary>
    /// Represents a single term of a dictionary entry.
    /// </summary>
    public sealed class RantDictionaryTerm
    {
        private string _pronunciation = string.Empty;
        private int _syllableCount;
        private string[] _syllables;
        private string _value;

        /// <summary>
        /// Intializes a new instance of the <see cref="RantDictionaryTerm" /> class with the specified value string.
        /// </summary>
        /// <param name="value">The value of the term.</param>
        /// <param name="splitIndex">The split index of the term value. Specify -1 for no split.</param>
        public RantDictionaryTerm(string value, int splitIndex = -1)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            ValueSplitIndex = splitIndex;
            _value = value;
        }

        /// <summary>
        /// Intializes a new instance of the <see cref="RantDictionaryTerm" /> class with the specified value and pronunciation
        /// strings.
        /// </summary>
        /// <param name="value">The value of the term.</param>
        /// <param name="pronunciation">The pronunciation of the term value.</param>
        public RantDictionaryTerm(string value, string pronunciation)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            _value = value;
            _pronunciation = pronunciation ?? string.Empty;
        }

        /// <summary>
        /// Intializes a new instance of the <see cref="RantDictionaryTerm" /> class with the specified value, pronunciation, and
        /// split indices.
        /// </summary>
        /// <param name="value">The value of the term.</param>
        /// <param name="pronunciation">The pronunciation of the term value.</param>
        /// <param name="valueSplitIndex">The split index of the term value. Specify -1 for no split.</param>
        /// <param name="pronSplitIndex">
        /// The split index of the term pronunciation string. Specify -1 for no split. Must be
        /// positive if the value is split and pronunciation data is present.
        /// </param>
        public RantDictionaryTerm(string value, string pronunciation, int valueSplitIndex, int pronSplitIndex)
        {
            if (value == null) throw new ArgumentNullException(nameof(value));
            if (valueSplitIndex < 0 != pronSplitIndex < 0 && !Util.IsNullOrWhiteSpace(pronunciation)) throw new ArgumentException(GetString("err-incomplete-term-split"));
            if (valueSplitIndex > value.Length)
                throw new ArgumentException(GetString("err-invalid-term-split"), nameof(valueSplitIndex));
            if (pronSplitIndex > pronunciation?.Length)
                throw new ArgumentException(GetString("err-invalid-term-split"), nameof(pronSplitIndex));

            _value = value;
            _pronunciation = pronunciation ?? string.Empty;
            ValueSplitIndex = valueSplitIndex;
            PronunciationSplitIndex = pronSplitIndex;
        }

        /// <summary>
        /// The value string of the term.
        /// </summary>
        public string Value
        {
            get { return _value; }
            set
            {
                if (value == null) throw new ArgumentNullException(nameof(value));
                _value = value;
            }
        }

        /// <summary>
        /// Determines whether the term is a split word.
        /// </summary>
        public bool IsSplit => ValueSplitIndex > -1;

		/// <summary>
		/// Gets the term value substring on the left side of the split.
		/// </summary>
		public string LeftSide => Value.Substring(0, ValueSplitIndex);

		/// <summary>
		/// Gets the term value substring on the right side of the split.
		/// </summary>
		public string RightSide => Value.Substring(ValueSplitIndex);

        /// <summary>
        /// Gets the split index of the term value.
        /// </summary>
        public int ValueSplitIndex { get; set; } = -1;

        /// <summary>
        /// Gets the split index of the term pronunciation string.
        /// </summary>
        public int PronunciationSplitIndex { get; set; } = -1;

        /// <summary>
        /// The pronunciation of the term.
        /// </summary>
        public string Pronunciation
        {
            get { return _pronunciation; }
            set
            {
                _pronunciation = value ?? string.Empty;
                if (_syllables != null) CreateSyllables();
            }
        }

        /// <summary>
        /// An array containing the individual syllables of the pronunciation string.
        /// </summary>
        public string[] Syllables => _syllables ?? CreateSyllables();

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

        private string[] CreateSyllables()
        {
            _syllables = _pronunciation.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            _syllableCount = _syllables.Length;
            return _syllables;
        }
    }
}