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
using System.IO;
using System.Reflection;
using System.Text;

using Rant.Core.Utilities;

namespace Rant.Core.Formatting
{
    internal static class Unicode
    {
        private static readonly Dictionary<uint, string> _cache = new Dictionary<uint, string>();

        private static readonly Dictionary<string, uint> _unicodeCharacterMap;

        static Unicode()
        {
            _unicodeCharacterMap = new Dictionary<string, uint>();
            var ass = typeof(Unicode).GetTypeInfo().Assembly;
            using (var stream = ass.GetManifestResourceStream("Rant.Core.Formatting.unicode_code_points.dat"))
            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (Util.IsNullOrWhiteSpace(line)) continue;
                    var entry = line.Split(',');
                    _unicodeCharacterMap[entry[0].Trim()] = Convert.ToUInt32(entry[1].Trim());
                }
            }
        }

        public static string GetByName(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (!_unicodeCharacterMap.TryGetValue(name, out uint codePoint))
            {
                var sb = new StringBuilder();
                bool space = false;
                foreach (char t in name)
                {
                    if (Char.IsLetterOrDigit(t))
                    {
                        if (space)
                        {
                            space = false;
                            if (sb.Length > 0) sb.Append(' ');
                        }
                        sb.Append(Char.ToUpper(t));
                        continue;
                    }

                    space = true;
                }

                if (!_unicodeCharacterMap.TryGetValue(sb.ToString(), out codePoint))
                    return String.Empty;
            }

            if (_cache.TryGetValue(codePoint, out string result)) return result;

            const uint minSurrogateCodePoint = 0x10000;
            const uint lowSurrogateMask = 0x3ff;
            const uint highSurrogateMask = lowSurrogateMask << 10;
            const uint lowSurrogateOffset = 0xDC00;
            const uint highSurrogateOffset = 0xD800;

            if (codePoint < minSurrogateCodePoint)
                return _cache[codePoint] = ((char)codePoint).ToString();

            uint sp = codePoint - minSurrogateCodePoint;

            result = _cache[codePoint] = new string(new[]
            {
                (char)(((sp & highSurrogateMask) >> 10) + highSurrogateOffset),
                (char)((sp & lowSurrogateMask) + lowSurrogateOffset)
            });

            return result;
        }
    }
}