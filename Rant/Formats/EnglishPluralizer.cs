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
using System.Linq;

using Rant.Core.Utilities;

namespace Rant.Formats
{
    /// <summary>
    /// Pluralizer for English nouns.
    /// </summary>
    public sealed class EnglishPluralizer : Pluralizer
    {
        private static readonly HashSet<char> consonants =
            new HashSet<char>(new[]
                { 'b', 'c', 'd', 'f', 'g', 'h', 'j', 'k', 'l', 'm', 'n', 'p', 'q', 'r', 's', 't', 'v', 'w', 'x', 'y', 'z' });

		private static readonly HashSet<char> hardConsonants =
			new HashSet<char>(new[] { 'j', 's', 'x', 'z' });

		private static readonly HashSet<string> compounds =
			new HashSet<string>(new[] {
				" of staff",
				"-in-law"
			});

		private static readonly Dictionary<string, string> irregulars =
			new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "man", "men" },
				{ "safe", "safes" },
				{ "foot", "feet" },
				{ "tooth", "teeth" },
				{ "antenna", "antennae" },
				{ "thesis", "theses" },
				{ "axis", "axes" },
				{ "basis", "bases" },
				{ "goose", "geese" },
				{ "louse", "lice" },
				{ "mouse", "mice" },
				{ "dormouse", "dormice" },
				{ "person", "people" },
				{ "die", "dice" },
				{ "index", "indices" },
				{ "matrix", "matrices" },
				{ "vertex", "vertices" },
				{ "criterion", "criteria" },
				{ "passerby", "passersby" },
				{ "ox", "oxen" },
				{ "fox", "foxes" },
				{ "piano", "pianos" },
				{ "alumnus", "alumni" },
				{ "cactus", "cacti" },
				{ "fungus", "fungi" },
				{ "focus", "foci" },
				{ "nucleus", "nuclei" },
				{ "radius", "radii" },
				{ "stimulus", "stimuli" },
				{ "child", "children" },
				{ "pro", "pros" },
				{ "zero", "zeros" },
				{ "photo", "photos" },
				{ "kimono", "kimonos" },
				{ "canto", "cantos" },
				{ "hetero", "heteros" },
				{ "crisis", "crises" },
				{ "testis", "testes" },
				{ "nemesis", "nemeses" },
				{ "genesis", "geneses" }
			};

        private static readonly HashSet<string> ignore = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "deer",
            "fish",
            "means",
            "offspring",
            "series",
            "sheep",
            "species",
            "trout",
            "bison",
            "buffalo",
            "moose",
            "pike",
            "plankton",
            "salmon",
            "squid",
            "swine",
            "swiss",
            "chinese"
        };

        /// <summary>
        /// Determines the plural form of the specified English noun.
        /// </summary>
        /// <param name="input">The singular form of the noun to pluralize.</param>
        /// <returns></returns>
        public override string Pluralize(string input)
        {
            if (Util.IsNullOrWhiteSpace(input)) return input;
            input = input.Trim().ToLowerInvariant();
            int l = input.Length;
            if (l == 1) return input.ToUpperInvariant() + "'s";
            if (ignore.Contains(input)) return input;
            string result;
            if (irregulars.TryGetValue(input, out result)) return result;
            if ((result = irregulars.Keys.FirstOrDefault(w => input.EndsWith(w))) != null)
                return input.Substring(0, l - result.Length) + irregulars[result];

			if (input.EndsWith("ff")) return input + "s";

			if (input[l - 1] == 'f') return input.Substring(0, l - 1) + "ves";

			if (input.EndsWith("fe")) return input.Substring(0, l - 2) + "ves";

			if (consonants.Contains(input[l - 2]))
			{
				// With nouns ending in o preceded by a consonant, the plural in many cases is spelled by adding -es...
				if (input.EndsWith("o")) return input + "es";

				// Nouns ending in a y preceded by a consonant usually drop the y and add -ies...
				if (input.EndsWith("y")) return input.Substring(0, l - 1) + "ies";
			}

            // Plurals of words ending in "man" end in "men"
            if (input.EndsWith("man")) return input.Substring(0, l - 2) + "en";

			// Add -es to words ending in a hard consonant or fricative
			if (input.EndsWith("ch") || input.EndsWith("sh") || hardConsonants.Contains(input[l - 1])) return input + "es";

            // Default
            return input + "s";
        }
    }
}