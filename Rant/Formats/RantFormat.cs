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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Rant.Core.Utilities;

namespace Rant.Formats
{
    /// <summary>
    /// Describes language-specific formatting instructions for localizing interpreter output.
    /// </summary>
    public sealed class RantFormat
    {
        /// <summary>
        /// English formatting.
        /// </summary>
        public static readonly RantFormat English;

		/// <summary>
		/// German formatting.
		/// </summary>
		public static readonly RantFormat German;

        static RantFormat()
        {
            English = new RantFormat();
			German = new RantFormat(CultureInfo.GetCultureInfo("de-DE"),
				new WritingSystem(new[]
				{
					'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm',
					'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z',
					'ä', 'ö', 'ü', 'ß'
				}, " ", new QuotationMarks('\u201e', '\u201c', '\u201a', '\u2018')),
				new string[0],
				new EnglishPluralizer(),
				new GermanNumberVerbalizer());
        }

        /// <summary>
        /// Creates a new RantFormat instance with default values.
        /// </summary>
        public RantFormat()
        {
        }

		/// <summary>
		/// Creates a new RantFormat instance with the specified configuration data.
		/// </summary>
		/// <param name="culture">The culture to associate with the format.</param>
		/// <param name="writingSystem">The writing system to use.</param>
		/// <param name="titleCaseExclusions">A collection of words to exclude from title case capitalization.</param>
		/// <param name="pluralizer">The pluralizer to use.</param>
		/// <param name="numVerbalizer">The number verbalizer to use.</param>
		public RantFormat(
			CultureInfo culture, WritingSystem writingSystem, IEnumerable<string> titleCaseExclusions, 
			Pluralizer pluralizer, NumberVerbalizer numVerbalizer)
        {
            Culture = culture;
			WritingSystem = writingSystem;
            foreach (string word in titleCaseExclusions) TitleCaseExclusions.Add(word);
            Pluralizer = pluralizer;
			NumberVerbalizer = numVerbalizer;
        }

        /// <summary>
        /// Gets the collection of words excluded from Title Case capitalization.
        /// </summary>
        private HashSet<string> TitleCaseExclusions { get; } = new HashSet<string>(
			new[] 
			{
				"a", "an", "the", "that", "where", "when", "for", "any", "or", "and",
				"of", "in", "at", "as", "into", "if", "are", "you", "why", "from",
				"with", "these", "those", "to"
			},
			StringComparer.OrdinalIgnoreCase);

        /// <summary>
		/// The writing system for the current format.
		/// </summary>
		public WritingSystem WritingSystem { get; } = new WritingSystem();

        /// <summary>
        /// The culture to format output strings with.
        /// </summary>
        public CultureInfo Culture { get; } = CultureInfo.InvariantCulture;

        /// <summary>
        /// The pluralizer used by the [plural] function to infer plural nouns.
        /// </summary>
        public Pluralizer Pluralizer { get; } = new EnglishPluralizer();

		/// <summary>
		/// The number verbalizer for the current format.
		/// </summary>
		public NumberVerbalizer NumberVerbalizer { get; } = new EnglishNumberVerbalizer();

        internal bool Excludes(string word) => TitleCaseExclusions.Contains(word);
    }
}