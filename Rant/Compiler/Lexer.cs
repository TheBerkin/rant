using System.Collections.Generic;
using System.Text.RegularExpressions;
using Rant.Stringes;
using Rant.Stringes.Tokens;

namespace Rant.Compiler
{
    internal static class Lexer
    {
        private const RegexOptions DefaultOptions = RegexOptions.Compiled | RegexOptions.ExplicitCapture;

        public static readonly Regex EscapeRegex = new Regex(@"\\((?<count>\d+((\.\d+)?[kMB])?),)?((?<code>[^u\s\r\n])|u(?<unicode>[0-9a-f]{4}))", DefaultOptions);
        public static readonly Regex RegexRegex = new Regex(@"//(.*?[^\\])?//i?", DefaultOptions);

        private static readonly Regex WhitespaceRegex = new Regex(@"\s+", DefaultOptions);
        private static readonly Regex BlackspaceRegex = new Regex(@"(^\s+|\s*[\r\n]+\s*|\s+$)", DefaultOptions | RegexOptions.Multiline);
        private static readonly Regex CommentRegex = new Regex(@"\s*#.*?(?=[\r\n]|$)", DefaultOptions | RegexOptions.Multiline);
        private static readonly Regex ConstantLiteralRegex = new Regex(@"""([^""]|"""")*""", DefaultOptions);

        internal static readonly LexerRules<TokenType> Rules;

        private static Stringe TruncatePadding(Stringe input)
        {
            var ls = input.LeftPadded ? input.TrimStart() : input;
            return ls.RightPadded ? ls.TrimEnd() : ls;
        }

        static Lexer()
        {
            Rules = new LexerRules<TokenType>
            {
                {EscapeRegex, TokenType.EscapeSequence},
                {RegexRegex, TokenType.Regex},
                {ConstantLiteralRegex, TokenType.ConstantLiteral},
                {"[", TokenType.LeftSquare}, {"]", TokenType.RightSquare},
                {"{", TokenType.LeftCurly}, {"}", TokenType.RightCurly},
                {"(", TokenType.LeftParen}, {")", TokenType.RightParen},
                {"<", TokenType.LeftAngle}, {">", TokenType.RightAngle},
                {"|", TokenType.Pipe},
                {";", TokenType.Semicolon},
                {":", TokenType.Colon},
                {"@", TokenType.At},
                {"?", TokenType.Question},
                {"::", TokenType.DoubleColon},
                {"?!", TokenType.Without},
                {"-", TokenType.Hyphen},
                {"!", TokenType.Exclamation},
                {"$", TokenType.Dollar},
                {"=", TokenType.Equal},
                {"&", TokenType.Ampersand},
                {CommentRegex, TokenType.Ignore, 3},
                {BlackspaceRegex, TokenType.Ignore, 2},
                {WhitespaceRegex, TokenType.Whitespace}
            };
            Rules.AddUndefinedCaptureRule(TokenType.Text, TruncatePadding);
            Rules.AddEndToken(TokenType.EOF);
            Rules.IgnoreRules.Add(TokenType.Ignore);
        }

        public static IEnumerable<Token<TokenType>> GenerateTokens(string input)
        {
            var reader = new StringeReader(input);
            while (!reader.EndOfStringe)
            {
                yield return reader.ReadToken(Rules);
            }
        }
    }
}