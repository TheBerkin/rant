using System.Collections.Generic;
using Rant.Arithmetic.Parselets;

namespace Rant.Arithmetic
{
    internal partial class Parser
    {
        private static readonly Dictionary<MathTokenType, IInfixParselet> InfixParselets;
        private static readonly Dictionary<MathTokenType, IPrefixParselet> PrefixParselets; 

        static Parser()
        {
            InfixParselets = new Dictionary<MathTokenType, IInfixParselet>
            {
                {MathTokenType.Equals, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.Plus, new BinaryOperatorParselet((int)Precedence.Sum, false)},
                {MathTokenType.Minus, new BinaryOperatorParselet((int)Precedence.Sum, false)},
                {MathTokenType.Asterisk, new BinaryOperatorParselet((int)Precedence.Product, false)},
                {MathTokenType.Slash, new BinaryOperatorParselet((int)Precedence.Product, false)},
                {MathTokenType.Caret, new BinaryOperatorParselet((int)Precedence.Exponent, false)},
                {MathTokenType.Modulo, new BinaryOperatorParselet((int)Precedence.Product, false)},
                {MathTokenType.Increment, new PostfixOperatorParselet((int)Precedence.Postfix)},
                {MathTokenType.Decrement, new PostfixOperatorParselet((int)Precedence.Postfix)},
                {MathTokenType.Swap, new BinaryOperatorParselet((int)Precedence.Assignment, false)},
                {MathTokenType.AddAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.SubAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.MulAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.DivAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.ModAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
                {MathTokenType.PowAssign, new BinaryOperatorParselet((int)Precedence.Assignment, true)},
            };

            PrefixParselets = new Dictionary<MathTokenType, IPrefixParselet>
            {
                {MathTokenType.Minus, new PrefixOperatorParselet((int)Precedence.Prefix)},
                {MathTokenType.Increment, new PrefixOperatorParselet((int)Precedence.Prefix)},
                {MathTokenType.Decrement, new PrefixOperatorParselet((int)Precedence.Prefix)},
                {MathTokenType.Number, new NumberParselet()},
                {MathTokenType.Name, new NameParselet()},
                {MathTokenType.Pipe, new AbsParselet()}
            };
        }
    }
}