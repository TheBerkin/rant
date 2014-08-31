﻿using System;
using System.Collections.Generic;
using System.Linq;

using Manhood.Compiler;

using Stringes.Tokens;

namespace Manhood
{
    internal partial class Interpreter
    {
        private static readonly Dictionary<TokenType, Func<Interpreter, SourceReader, State, bool>> TokenFuncs = new Dictionary<TokenType, Func<Interpreter, SourceReader, State, bool>>
        {
            {TokenType.LeftCurly, DoBlock},
            {TokenType.LeftSquare, DoTag},
            {TokenType.EscapeSequence, DoEscape},
            {TokenType.ConstantLiteral, DoConstant}
        };

        private static bool DoConstant(Interpreter interpreter, SourceReader reader, State state)
        {
            state.Output.Write(Util.UnescapeConstantLiteral(reader.ReadToken().Value));
            return false;
        }

        private static bool DoEscape(Interpreter interpreter, SourceReader reader, State state)
        {
            state.Output.Write(Util.Unescape(reader.ReadToken().Value, interpreter.RNG));
            return false;
        }

        private bool DoElement(Token<TokenType> token, SourceReader reader, State state)
        {
            Func<Interpreter, SourceReader, State, bool> func;
            if (TokenFuncs.TryGetValue(token.Identifier, out func)) return func(this, reader, state);
            Print(token.Value);
            reader.Position++;
            return false;
        }

        private static bool DoTag(Interpreter interpreter, SourceReader reader, State state)
        {
            reader.Take(TokenType.LeftSquare);
            var name = reader.ReadToken();
            if (!Util.ValidateName(name.Value))
                throw new ManhoodException(reader.Source, name, "Invalid tag name '" + name.Value + "'");
            bool none = false;
            if (!reader.Take(TokenType.Colon))
            {
                if (!reader.Take(TokenType.RightSquare))
                    throw new ManhoodException(reader.Source, name, "Expected ':' or ']' after tag name.");
                none = true;
            }

            if (none)
            {
                state.AddBlueprint(new TagBlueprint(interpreter, reader.Source, name, 0));
            }
            else
            {
                var items = reader.ReadItemsToScopeClose(TokenType.LeftSquare, TokenType.RightSquare,
                    TokenType.Semicolon, BracketPairs.All).ToArray();

                foreach (var item in items)
                {
                    interpreter.PushState(State.CreateDistinct(reader.Source, item, interpreter));
                }

                state.AddBlueprint(new TagBlueprint(interpreter, reader.Source, name, items.Length));
            }
            return true;
        }

        private static bool DoBlock(Interpreter interpreter, SourceReader reader, State state)
        {
            var items = reader.ReadMultiItemScope(TokenType.LeftCurly, TokenType.RightCurly, TokenType.Pipe, BracketPairs.All).ToArray();
            var attribs = interpreter.PendingBlockAttribs;
            interpreter._blockAttribs = new BlockAttribs();

            if (!items.Any()) return false;

            state.AddBlueprint(
                new RepeaterBlueprint(interpreter,
                    new Repeater(attribs.Repetitons == Repeater.Each ? items.Length : attribs.Repetitons),
                    items));
            return true;
        }


    }
}