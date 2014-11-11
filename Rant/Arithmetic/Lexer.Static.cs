using System.Text.RegularExpressions;
using Rant.Stringes.Tokens;

namespace Rant.Arithmetic
{
    internal partial class Lexer
    {
        public static LexerRules<MathTokenType> Rules;
		
        static Lexer()
        {
            Rules = new LexerRules<MathTokenType>
            {
                {"+", MathTokenType.Plus},
				{"-", MathTokenType.Minus},
				{"*", MathTokenType.Asterisk},
				{"/", MathTokenType.Slash},
				{"^", MathTokenType.Caret},
				{"(", MathTokenType.LeftParen},
				{")", MathTokenType.RightParen},
				{"++", MathTokenType.Increment},
				{"--", MathTokenType.Decrement},
				{"%", MathTokenType.Modulo},
				{"=", MathTokenType.Equals},
                {"$=", MathTokenType.Swap},
                {"+=", MathTokenType.AddAssign},
                {"-=", MathTokenType.SubAssign},
                {"*=", MathTokenType.MulAssign},
                {"/=", MathTokenType.DivAssign},
                {"%=", MathTokenType.ModAssign},
                {"^=", MathTokenType.PowAssign},
                {"|", MathTokenType.Pipe},
                {new Regex(@"(\d+(\.\d+)?|\.\d+)"), MathTokenType.Number},
                {new Regex(@"[a-zA-Z_][a-zA-Z0-9_]*"), MathTokenType.Name}
            };
            Rules.AddEndToken(MathTokenType.End);
        }
    }
}