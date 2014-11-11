using System.Collections.Generic;
using System.Text.RegularExpressions;
using Rant.Stringes;
using Rant.Stringes.Tokens;

namespace Rant.Vocabulary
{
    internal static class Dic2Lexer
    {
        private const RegexOptions DicRegexOptions = RegexOptions.Multiline | RegexOptions.Compiled;

        private static readonly LexerRules<DicTokenType> Rules;

        static Dic2Lexer()
        {
            Rules = new LexerRules<DicTokenType>
            {
                {new Regex(@"\#\s*(?<value>[^\r]*)[\s\r]*$", DicRegexOptions), DicTokenType.Directive, 2},
                {new Regex(@"\|\s*(?<value>[^\r]*)[\s\r]$", DicRegexOptions), DicTokenType.Property, 2},
                {new Regex(@"\>\s*(?<value>[^\r]*)[\s\r]$", DicRegexOptions), DicTokenType.Entry, 2},
                {new Regex(@"\s+"), DicTokenType.Ignore}
            };
            Rules.AddEndToken(DicTokenType.EOF);
            Rules.IgnoreRules.Add(DicTokenType.Ignore);
        }

        public static IEnumerable<Token<DicTokenType>> Tokenize(string data)
        {
            Token<DicTokenType> token;
            var reader = new StringeReader(data);
            while ((token = reader.ReadToken(Rules)).Identifier != DicTokenType.EOF)
            {
                yield return token;
            }
        }
    }

    internal enum DicTokenType
    {
        Directive,
        Entry,
        Property,
        Ignore,
        EOF
    }
}