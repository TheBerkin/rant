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
using System.Text;

namespace Rant.Vocabulary.Utilities
{
    /// <summary>
    /// Diffs your marks.
    /// </summary>
    internal sealed class Diff
    {
        private readonly Rule[] _rules;

        /// <summary>
        /// Creates a new Diffmark pattern for you to enjoy.
        /// </summary>
        /// <param name="patternString">The pattern string.</param>
        public Diff(string patternString)
        {
            if (patternString == null)
                throw new ArgumentException("Pattern string cannot be null.");

            Pattern = patternString;
            _rules = Lexer.Lex(patternString).Select(tokens => Rule.Parse(tokens.ToArray())).ToArray();
        }

        /// <summary>
        /// The pattern string for the diff.
        /// </summary>
        public string Pattern { get; }

        /// <summary>
        /// Applies the pattern to a string.
        /// </summary>
        /// <param name="baseString">The string to apply the pattern to.</param>
        /// <returns></returns>
        public string Mark(string baseString)
        {
            foreach (var rule in _rules)
            {
                switch (rule.Type)
                {
                    case DiffRuleType.Add:
                        baseString = rule.Prepend
                            ? rule.ConcatString + baseString
                            : baseString + rule.ConcatString;
                        continue;
                    case DiffRuleType.Subtract:
                        baseString = Cut(baseString, rule.Factor, rule.Prepend);
                        baseString = rule.Prepend
                            ? rule.ConcatString + baseString
                            : baseString + rule.ConcatString;
                        continue;
                    case DiffRuleType.ReplaceWord:
                        baseString = ReplaceWord(baseString, rule.ConcatString, rule.Factor, rule.Prepend);
                        continue;
                }
            }
            return baseString;
        }

        /// <summary>
        /// Transforms one string to another.
        /// </summary>
        /// <param name="baseString">The base string to transform.</param>
        /// <param name="pattern">The Diffmark pattern to apply to the string.</param>
        /// <returns></returns>
        public static string Mark(string baseString, string pattern)
        {
            return new Diff(pattern).Mark(baseString);
        }

        internal static string Cut(string baseString, int factor, bool prepend)
        {
            if (factor > baseString.Length) return string.Empty;
            return prepend ? baseString.Substring(factor) : baseString.Substring(0, baseString.Length - factor);
        }

        internal static string ReplaceWord(string baseString, string replacement, int factor, bool prepend)
        {
            int a = 0;
            int b = 0;
            int currentWordIndex = 0;
            if (prepend)
            {
                while (currentWordIndex < factor)
                {
                    a = b;
                    while (a < baseString.Length && !char.IsLetterOrDigit(baseString[a]))
                        a++;

                    if (a >= baseString.Length) return baseString;

                    b = a;
                    while (b < baseString.Length && char.IsLetterOrDigit(baseString[b]))
                        b++;

                    currentWordIndex++;
                }
            }
            else
            {
                a = b = baseString.Length;
                while (currentWordIndex < factor)
                {
                    b = a;
                    while (b > 0 && !char.IsLetterOrDigit(baseString[b - 1]))
                        b--;

                    if (b <= 0) return baseString;

                    a = b;
                    while (a > 0 && char.IsLetterOrDigit(baseString[a - 1]))
                        a--;

                    currentWordIndex++;
                }
            }
            return string.Concat(baseString.Substring(0, a), replacement, baseString.Substring(b));
        }

        /// <summary>
        /// Derives a Diffmark pattern that can transform the specified 'before' string to the specified 'after' string.
        /// </summary>
        /// <param name="before">The string before it is transformed.</param>
        /// <param name="after">The string after it is transformed.</param>
        /// <returns></returns>
        public static string Derive(string before, string after)
        {
            if (before == null || after == null)
                throw new ArgumentException("Input strings can't be null.");
            if (before == after) return "";
            if (after.Length == 0) return new string('-', before.Length);
            if (before.Length == 0) return after;

            var sb = new StringBuilder();
            bool isCut = after.Length < before.Length;

            int lm = LongestLeftMatch(before, after);
            int rm = LongestRightMatch(before, after);

            int lcsStartBefore = 0;
            int lcsStartAfter = 0;
            int lcsLength = 0;

            if (isCut)
            {
                if (lm > 0) // Something was appended
                {
                    sb.Append(new string('-', before.Length - lm))
                        .Append(after.Substring(lm, after.Length - lm));
                }
                else if (rm > 0) // Something was prepended
                {
                    sb.Append(after.Substring(0, after.Length - rm))
                        .Append(after.Length - rm == 0 && before.Length - rm > 0 ? "|" : "")
                        .Append(new string('-', before.Length - rm));
                }
                else
                {
                    if (LongestCommonSubstring(before, after, out lcsStartBefore, out lcsStartAfter, out lcsLength))
                    {
                        string beforeLeft = before.Substring(0, lcsStartBefore);
                        string afterLeft = after.Substring(0, lcsStartAfter);
                        lm = LongestLeftMatch(beforeLeft, afterLeft);

                        sb.Append(afterLeft.Substring(0, afterLeft.Length - lm))
                            .Append(lm < beforeLeft.Length
                                ? (afterLeft.Length - lm == 0 && beforeLeft.Length - lm > 0 ? "|" : "") +
                                  new string('-', beforeLeft.Length - lm)
                                : "+")
                            .Append(';');

                        string beforeRight = before.Substring(lcsStartBefore + lcsLength);
                        string afterRight = after.Substring(lcsStartAfter + lcsLength);
                        rm = LongestRightMatch(beforeRight, afterRight);

                        sb.Append(new string('-', beforeRight.Length - rm))
                            .Append(afterRight.Substring(0, afterRight.Length - rm));
                    }
                    else if (before.Contains(" "))
                    {
                        sb.Append(new string('-', before.Length))
                            .Append(after);
                    }
                    else
                        sb.Append('*').Append(after);
                }
            }
            else
            {
                if (lm > 0) // Something was appended
                {
                    sb.Append(new string('-', before.Length - lm))
                        .Append(after.Substring(lm, after.Length - lm));
                }
                else if (rm > 0) // Something was prepended
                {
                    sb.Append(after.Substring(0, after.Length - rm))
                        .Append(rm < before.Length
                            ? (after.Length - rm == 0 && before.Length - rm > 0 ? "|" : "") + new string('-', before.Length - rm)
                            : "+");
                }
                else
                {
                    if (LongestCommonSubstring(before, after, out lcsStartBefore, out lcsStartAfter, out lcsLength))
                    {
                        string beforeLeft = before.Substring(0, lcsStartBefore);
                        string afterLeft = after.Substring(0, lcsStartAfter);
                        lm = LongestLeftMatch(beforeLeft, afterLeft);
                        sb.Append(afterLeft.Substring(0, afterLeft.Length - lm))
                            .Append(lm < beforeLeft.Length
                                ? (afterLeft.Length - lm == 0 && beforeLeft.Length - lm > 0 ? "|" : "") +
                                  new string('-', beforeLeft.Length - lm)
                                : "+")
                            .Append(';');

                        string beforeRight = lcsStartBefore + lcsLength >= before.Length
                            ? ""
                            : before.Substring(lcsStartBefore + lcsLength);
                        string afterRight = lcsStartAfter + lcsLength >= after.Length ? "" : after.Substring(lcsStartAfter + lcsLength);
                        rm = LongestRightMatch(beforeRight, afterRight);

                        sb.Append(new string('-', beforeRight.Length - rm))
                            .Append(afterRight.Substring(0, afterRight.Length - rm));
                    }
                    else if (before.Contains(" "))
                    {
                        sb.Append(new string('-', before.Length))
                            .Append(after);
                    }
                    else
                        sb.Append('*').Append(after);
                }
            }

            return sb.ToString();
        }

        internal static bool LongestCommonSubstring(string a, string b,
            out int start_a, out int start_b, out int length)
        {
            start_a = start_b = length = 0;

            if (a == null || b == null) return false;
            if (a.Length * b.Length == 0) return false;

            string minor, major;
            int minorStart = 0;
            int majorStart = 0;
            if (a.Length >= b.Length)
            {
                minor = a;
                major = b;
            }
            else
            {
                major = a;
                minor = b;
            }

            int matchIndex = 0;
            for (int i = 0; i < minor.Length; i++)
            for (int j = i + 1; j <= minor.Length; j++)
            {
                if (j - i > length
                    && (matchIndex = major.IndexOf(minor.Substring(i, j - i), StringComparison.Ordinal)) > -1)
                {
                    majorStart = matchIndex;
                    minorStart = i;
                    length = j - i;
                }
            }

            if (a.Length >= b.Length)
            {
                start_a = minorStart;
                start_b = majorStart;
            }
            else
            {
                start_a = majorStart;
                start_b = minorStart;
            }

            return length > 0;
        }

        private static int LongestLeftMatch(string a, string b)
        {
            if (a.Length * b.Length == 0) return 0;
            int l = Math.Min(a.Length, b.Length);

            for (int i = 0; i < l; i++)
                if (a[i] != b[i]) return i;
            return l;
        }

        private static int LongestRightMatch(string a, string b)
        {
            if (a.Length * b.Length == 0) return 0;
            int l = Math.Min(a.Length, b.Length);
            for (int i = 0; i < l; i++)
                if (a[a.Length - 1 - i] != b[b.Length - 1 - i]) return i;
            return l;
        }

        private class Rule
        {
            private static readonly Dictionary<DM, DiffRuleType> RuleMap = new Dictionary<DM, DiffRuleType>
            {
                { DM.ReplaceWord, DiffRuleType.ReplaceWord },
                { DM.Subtract, DiffRuleType.Subtract },
                { DM.Add, DiffRuleType.Add }
            };

            public readonly string ConcatString;
            public readonly int Factor;
            public readonly bool Prepend;
            public readonly DiffRuleType Type;

            private Rule(string concatString, bool prepend, DiffRuleType type, int factor)
            {
                ConcatString = concatString;
                Prepend = prepend;
                Type = type;
                Factor = factor;
            }

            public static Rule Parse(Token[] tokens)
            {
                DiffRuleType ruleType;
                DM op;
                bool prepend = !RuleMap.TryGetValue(op = tokens[0].Type, out ruleType);
                if (prepend && !RuleMap.TryGetValue(op = tokens[tokens.Length - 1].Type, out ruleType))
                {
                    prepend = false;
                    ruleType = DiffRuleType.Add;
                }

                int factor = 0;

                if (ruleType != DiffRuleType.Add)
                {
                    if (prepend)
                        factor += tokens.Reverse().TakeWhile(t => t.Type == op).Count();
                    else
                        factor += tokens.TakeWhile(t => t.Type == op).Count();
                }
                else
                    factor++;

                var sb = new StringBuilder();

                foreach (var token in
                    tokens.SkipWhile(t => RuleMap.ContainsKey(t.Type))
                        .Reverse()
                        .SkipWhile(t => RuleMap.ContainsKey(t.Type))
                        .Reverse())
                    sb.Append(token.Value);

                return new Rule(sb.ToString(), prepend, ruleType, factor);
            }
        }

        private enum DiffRuleType
        {
            Add,
            Subtract,
            ReplaceWord
        }

        private static class Lexer
        {
            private static IEnumerable<Token> GetTokens(string patternString)
            {
                patternString = patternString.Trim();
                var text = new StringBuilder();
                Token nextToken = null;
                for (int i = 0; i < patternString.Length; i++)
                {
                    switch (patternString[i])
                    {
                        case '\\':
                            nextToken = new Token(DM.Special, Escape(patternString[++i]));
                            break;
                        case '|':
                            nextToken = new Token(DM.Special, "");
                            break;
                        case '+':
                            nextToken = new Token(DM.Add, "+");
                            break;
                        case '-':
                            nextToken = new Token(DM.Subtract, "-");
                            break;
                        case '*':
                            nextToken = new Token(DM.ReplaceWord, "*");
                            break;
                        case ';':
                            nextToken = new Token(DM.Delimiter, ";");
                            break;
                        case ' ':
                            if (text.Length > 0) goto default;
                            continue;
                        default:
                            text.Append(patternString[i]);
                            continue;
                    }
                    if (text.Length > 0)
                    {
                        yield return new Token(DM.Text, text.ToString().Trim());
                        text.Length = 0;
                    }
                    yield return nextToken;
                    nextToken = null;
                }
                if (text.Length > 0)
                    yield return new Token(DM.Text, text.ToString().Trim());
            }

            public static IEnumerable<IEnumerable<Token>> Lex(string patternString)
            {
                var tokens = GetTokens(patternString);
                var list = new List<Token>();
                foreach (var token in tokens)
                {
                    if (token.Type == DM.Delimiter && list.Any())
                    {
                        yield return list.ToArray();
                        list.Clear();
                    }
                    else
                        list.Add(token);
                }
                if (list.Any()) yield return list.ToArray();
            }

            internal static string Escape(char escapeChar)
            {
                switch (escapeChar)
                {
                    case 'n':
                        return "\n";
                    case 's':
                        return " ";
                    case 'r':
                        return "\r";
                    case 't':
                        return "\t";
                    default:
                        return escapeChar.ToString();
                }
            }
        }

        private class Token
        {
            public readonly DM Type;
            public readonly string Value;

            public Token(DM type, string value)
            {
                Type = type;
                Value = value;
            }
        }

        private enum DM
        {
            Add,
            Subtract,
            ReplaceWord,
            Special,
            Text,
            Delimiter
        }
    }
}