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

using System.Globalization;
using System.Linq;
using System.Text;

namespace Rant.Core.Formatting
{
	internal static class Numerals
    {
        public const int MaxRomanValue = 3999;

        private static readonly string[][] RomanNumerals =
        {
            new[] { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" },
            new[] { "", "X", "XX", "XXX", "XL", "L", "LX", "LXX", "LXXX", "XC" },
            new[] { "", "C", "CC", "CCC", "CD", "D", "DC", "DCC", "DCCC", "CM" },
            new[] { "", "M", "MM", "MMM" }
        };

        public static string ToRoman(double number, bool lowerCase = false)
        {
            if (number <= 0 || number > MaxRomanValue || number % 1 > 0) return "?";
            var intArr =
                number.ToString(CultureInfo.InvariantCulture)
                    .Reverse()
                    .Select(c => int.Parse(c.ToString()))
                    .ToArray();
            var sb = new StringBuilder();
            for (int i = intArr.Length; i-- > 0;)
                sb.Append(RomanNumerals[i][intArr[i]]);
            return lowerCase ? sb.ToString().ToLower() : sb.ToString();
        }
    }
}