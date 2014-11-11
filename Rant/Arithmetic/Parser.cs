using System;
using System.Collections.Generic;
using System.Linq;
using Rant.Arithmetic.Parselets;
using Rant.Stringes;
using Rant.Stringes.Tokens;

namespace Rant.Arithmetic
{
    internal partial class Parser
    {
        private int _pos;
        private string _src;
        private readonly Token<MathTokenType>[] _tokens;

        public Parser(IEnumerable<Token<MathTokenType>> tokens)
        {
            _pos = 0;
            _tokens = tokens.ToArray();
        }

        public string Source
        {
            get { return _src; }
        }

        public static double Calculate(Interpreter ii, string expression)
        {
            double result = 0;
            foreach (var expr in expression.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var p = new Parser(new Lexer(expr.ToStringe()));
                result = p.ParseExpression().Evaluate(p, ii);
            }
            return result;
        }

        public Expression ParseExpression(int precedence = 0)
        {
            var token = Take();

            IPrefixParselet prefixParselet;
            if (!PrefixParselets.TryGetValue(token.Identifier, out prefixParselet))
            {
                throw new RantException(_src, token, "Invalid expression '" + token.Value + "'.");
            }

            var left = prefixParselet.Parse(this, token);
            // Break when the next token's precedence is less than or equal to the current precedence
            // This will assure that higher precedence operations like multiplication are parsed before lower operations.
            while (GetPrecedence() > precedence)
            {
                token = Take();
                IInfixParselet infix;
                if (!InfixParselets.TryGetValue(token.Identifier, out infix))
                {
                    throw new RantException(_src, token, "Invalid operator '" + token.Value + "'.");
                }

                // Replace the left expression with the next parsed expression.
                left = infix.Parse(this, left, token);
            }

            return left;
        }

        /// <summary>
        /// Returns the precedence of the next infix operator, or 0 if there is none.
        /// </summary>
        /// <returns></returns>
        private int GetPrecedence()
        {
            if (_pos == _tokens.Length) return 0;
            IInfixParselet infix;
            InfixParselets.TryGetValue(Peek().Identifier, out infix);
            return infix != null ? infix.Precedence : 0;
        }

        public Token<MathTokenType> Peek(int distance = 0)
        {
            if (_pos == _tokens.Length) return null;
            if (distance < 0) throw new ArgumentOutOfRangeException("distance");
            return _tokens[_pos + distance];
        }

        public Token<MathTokenType> Take(MathTokenType type)
        {
            var token = Take();
            if (token.Identifier != type)
            {
                throw new RantException(_src, token, String.Concat("Expression expected ", type, ", but found ", token.Identifier, "."));
            }
            return token;
        }

        public Token<MathTokenType> Take()
        {
            if (_pos < _tokens.Length) return _tokens[_pos++];
            return null;
        }
    }
}