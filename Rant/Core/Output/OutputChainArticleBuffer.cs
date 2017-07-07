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

namespace Rant.Core.Output
{
    internal class OutputChainArticleBuffer : OutputChainBuffer
    {
        private static readonly HashSet<char> vowels =
            new HashSet<char>(new[] { 'a', 'e', 'i', 'o', 'u', 'A', 'E', 'I', 'O', 'U', 'é', 'É' });

        private static readonly string[] ignorePrefixes =
            { "uni", "use", "uri", "urol", "U.", "one", "uvu", "eul", "euk", "eur" };

        private static readonly string[] allowPrefixes =
            { "honest", "honor", "hour", "8" };

        private static readonly string[] ignoreWords = { "u" };

        private static readonly string[] allowWords =
            { "f", "fbi", "fcc", "fda", "x", "l", "m", "n", "s", "h" };

        public OutputChainArticleBuffer(Sandbox sb, OutputChainBuffer prev) : base(sb, prev)
        {
            Initialize();
        }

        public OutputChainArticleBuffer(Sandbox sb, OutputChainBuffer prev, OutputChainBuffer targetOrigin)
            : base(sb, prev, targetOrigin)
        {
            Initialize();
        }

        private void Initialize()
        {
            Print("a");
        }

        protected override void OnNextBufferChange()
        {
            if (Next.Length == 0) return;
            var sb = Next.Buffer;
            char c;
            int start = -1;
            int end = 0;
            for (int i = 0; i < sb.Length; i++)
            {
                c = sb[i];
                if (start == -1)
                {
                    if (char.IsWhiteSpace(c) || char.IsSeparator(c)) continue; // Must be padding, skip it
                    if (!char.IsLetterOrDigit(c)) continue;
                    start = i;
                    if (i == sb.Length - 1) end = start + 1; // Word is one character long
                }
                else
                {
                    end = i;
                    if (!char.IsLetterOrDigit(c)) break;
                    if (i == sb.Length - 1) end++; // Consume character if it's the last one in the buffer
                }
            }

            if (start == -1) return;

            var buffer = new char[end - start];
            sb.CopyTo(start, buffer, 0, end - start);
            Clear();
            Print(CheckRules(new string(buffer)) ? "an" : "a");
        }

        private static bool CheckRules(string value)
        {
            if (string.IsNullOrEmpty(value)) return false;
            return
                allowWords.Any(word => string.Equals(word, value, StringComparison.OrdinalIgnoreCase))
                || allowPrefixes.Any(pfx => value.StartsWith(pfx, StringComparison.OrdinalIgnoreCase))
                || vowels.Contains(value[0])
                && !ignorePrefixes.Any(pfx => value.StartsWith(pfx, StringComparison.OrdinalIgnoreCase))
                && !ignoreWords.Any(word => string.Equals(word, value, StringComparison.OrdinalIgnoreCase));
        }
    }
}