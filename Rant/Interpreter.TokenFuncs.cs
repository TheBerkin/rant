using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Rant.Arithmetic;
using Rant.Blueprints;
using Rant.Compiler;
using Rant.Vocabulary;
using Rant.Stringes.Tokens;

namespace Rant
{
    internal delegate bool TokenFunc(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, Interpreter.State state);

    internal partial class Interpreter
    {
        private static readonly Dictionary<TokenType, TokenFunc> TokenFuncs = new Dictionary<TokenType, TokenFunc>
        {
            {TokenType.LeftCurly, DoBlock},
            {TokenType.LeftSquare, DoTag},
            {TokenType.LeftParen, DoMath},
            {TokenType.LeftAngle, DoQuery},
            {TokenType.EscapeSequence, DoEscape},
            {TokenType.ConstantLiteral, DoConstant},
            {TokenType.Text, DoText}
        };

        private static bool DoMath(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            bool isStatement = reader.Take(TokenType.At);
            var tokens = reader.ReadToScopeClose(TokenType.LeftParen, TokenType.RightParen, BracketPairs.All);
            interpreter.PushState(State.CreateSub(reader.Source, tokens, interpreter));
            state.AddPreBlueprint(new DelegateBlueprint(interpreter, _ =>
            {
                var v = Parser.Calculate(_, _.PopResultString());
                if (!isStatement)
                {
                    _.Print(_.FormatNumber(v));
                }
                return false;
            }));
            return true;
        }

        private static bool DoQuery(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            reader.SkipSpace();

            bool storeMacro = false;
            bool macroIsGlobal = false;
            string macroName = null;

            // Check if this is a macro
            if (reader.Take(TokenType.At))
            {
                reader.SkipSpace();

                var macroNameToken = reader.Read(TokenType.Text, "query macro name");
                if (!Util.ValidateName(macroNameToken.Value))
                    throw new RantException(reader.Source, macroNameToken, "Invalid macro name.");

                macroName = macroNameToken.Value;

                reader.SkipSpace();

                // Check if the macro is a definition or a call.
                // A definition will start with a colon ':' or equals '=' after the name. A call will only consist of the name.
                switch (reader.ReadToken().Identifier)
                {
                    case TokenType.Colon: // Local definition
                    {
                        storeMacro = true;
                    }
                    break;
                    case TokenType.Equal: // Global definition
                    {
                        storeMacro = true;
                        macroIsGlobal = true;
                    }
                    break;
                    case TokenType.RightAngle: // Call
                    {
                        Query q;
                        if (!interpreter.LocalQueryMacros.TryGetValue(macroName, out q) && !interpreter.Engine.GlobalQueryMacros.TryGetValue(macroName, out q))
                        {
                            throw new RantException(reader.Source, macroNameToken, "Nonexistent query macro '" + macroName + "'");
                        }
                        interpreter.Print(interpreter.Engine.Vocabulary.Query(interpreter.RNG, q, interpreter.CarrierSyncState));
                        return false;
                    }
                }
            }

            reader.SkipSpace();
            var namesub = reader.Read(TokenType.Text, "list name").Split(new[] { '.' }, 2).ToArray();
            reader.SkipSpace();

            bool exclusive = reader.Take(TokenType.Dollar);
            List<Tuple<bool, string>> cfList = null;
            List<Tuple<bool, string>[]> classFilterList = null;
            List<Tuple<bool, Regex>> regList = null;
            Carrier carrier = null;

            Token<TokenType> queryToken = null;

            // Read query arguments
            while (true)
            {
                reader.SkipSpace();
                if (reader.Take(TokenType.Hyphen))
                {
                    // Initialize the filter list.
                    (cfList ?? (cfList = new List<Tuple<bool, string>>())).Clear();

                    do
                    {
                        bool notin = reader.Take(TokenType.Exclamation);
                        if (notin && exclusive)
                            throw new RantException(reader.Source, reader.PrevToken, "Cannot use the '!' modifier on exclusive class filters.");
                        cfList.Add(Tuple.Create(!notin, reader.Read(TokenType.Text, "class identifier").Value.Trim()));
                    } while (reader.Take(TokenType.Pipe));
                    (classFilterList ?? (classFilterList = new List<Tuple<bool, string>[]>())).Add(cfList.ToArray());
                }
                else if (reader.Take(TokenType.Question))
                {
                    queryToken = reader.Read(TokenType.Regex, "regex");
                    (regList ?? (regList = new List<Tuple<bool, Regex>>())).Add(Tuple.Create(true, Util.ParseRegex(queryToken.Value)));
                }
                else if (reader.Take(TokenType.Without))
                {
                    queryToken = reader.Read(TokenType.Regex, "regex");
                    (regList ?? (regList = new List<Tuple<bool, Regex>>())).Add(Tuple.Create(false, Util.ParseRegex(queryToken.Value)));
                }
                else if (reader.Take(TokenType.DoubleColon)) // Start of carrier
                {
                    reader.SkipSpace();

                    CarrierSyncType type;
                    Token<TokenType> typeToken;

                    switch ((typeToken = reader.ReadToken()).Identifier)
                    {
                        case TokenType.Exclamation:
                            type = CarrierSyncType.Unique;
                            break;
                        case TokenType.Equal:
                            type = CarrierSyncType.Match;
                            break;
                        case TokenType.Ampersand:
                            type = CarrierSyncType.Rhyme;
                            break;
                        default:
                            throw new RantException(reader.Source, typeToken, "Unrecognized token '" + typeToken.Value + "' in carrier.");
                    }

                    reader.SkipSpace();

                    carrier = new Carrier(type, reader.Read(TokenType.Text, "carrier sync ID").Value, 0, 0);

                    reader.SkipSpace();

                    if (!reader.Take(TokenType.RightAngle))
                    {
                        throw new RantException(reader.Source, queryToken, "Expected '>' after carrier. (The carrier should be your last query argument!)");
                    }
                    break;
                }
                else if (reader.Take(TokenType.RightAngle))
                {
                    break;
                }
                else if (!reader.SkipSpace())
                {
                    var t = !reader.End ? reader.ReadToken() : null;
                    throw new RantException(reader.Source, t, t == null ? "Unexpected end-of-file in query." : "Unexpected token '" + t.Value + "' in query.");
                }
            }

            var query = new Query(
                namesub[0].Value.Trim(),
                namesub.Length == 2 ? namesub[1].Value : "",
                carrier, exclusive, classFilterList, regList);

            if (storeMacro)
            {
                if (macroIsGlobal)
                {
                    interpreter.Engine.GlobalQueryMacros[macroName] = query;
                }
                else
                {
                    interpreter.LocalQueryMacros[macroName] = query;
                }
                return false;
            }

            // Query dictionary and print result
            interpreter.Print(interpreter.Engine.Vocabulary.Query(interpreter.RNG, query, interpreter.CarrierSyncState));

            return false;
        }

        private static bool DoText(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            interpreter.Print(firstToken.Value);
            return false;
        }

        private static bool DoConstant(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            interpreter.Print(Util.UnescapeConstantLiteral(firstToken.Value));
            return false;
        }

        private static bool DoEscape(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            interpreter.Print(Util.Unescape(firstToken.Value, interpreter, interpreter.RNG));
            return false;
        }

        private static bool DoTag(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            var name = reader.ReadToken();

            // Check if metapattern
            if (name.Identifier == TokenType.Question)
            {
                state.AddPreBlueprint(new MetapatternBlueprint(interpreter));
                interpreter.PushState(State.CreateSub(reader.Source, reader.ReadToScopeClose(TokenType.LeftSquare, TokenType.RightSquare, BracketPairs.All), interpreter));
                return true;
            }

            // Check if replacer
            if (name.Identifier == TokenType.Regex)
            {
                return DoReplacer(name, interpreter, reader, state);
            }

            if (name.Identifier == TokenType.Dollar)
            {
                return reader.IsNext(TokenType.Text) ? DoSubCall(name, interpreter, reader, state) : DoSubDefinition(name, interpreter, reader, state);
            }

            if (!Util.ValidateName(name.Value.Trim()))
                throw new RantException(reader.Source, name, "Invalid tag name '" + name.Value + "'");

            bool none = false;
            if (!reader.Take(TokenType.Colon))
            {
                if (!reader.Take(TokenType.RightSquare))
                    throw new RantException(reader.Source, name, "Expected ':' or ']' after tag name.");
                none = true;
            }

            if (none)
            {
                state.AddPreBlueprint(new FuncTagBlueprint(interpreter, reader.Source, name));
            }
            else
            {
                var items = reader.ReadItemsToClosureTrimmed(TokenType.LeftSquare, TokenType.RightSquare,
                    TokenType.Semicolon, BracketPairs.All).ToArray();

                state.AddPreBlueprint(new FuncTagBlueprint(interpreter, reader.Source, name, items));
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoSubCall(Token<TokenType> first, Interpreter interpreter, PatternReader reader, State state)
        {
            var name = reader.ReadToken();
            if (!Util.ValidateName(name.Value))
                throw new RantException(reader.Source, name, "Invalid subroutine name '" + name.Value + "'");
            
            bool none = false;

            if (!reader.Take(TokenType.Colon))
            {
                if (!reader.Take(TokenType.RightSquare))
                    throw new RantException(reader.Source, name, "Expected ':' or ']' after subroutine name.");
                
                none = true;
            }

            IEnumerable<Token<TokenType>>[] args = null;
            Subroutine sub = null;

            if (none)
            {
                if((sub = interpreter.Engine.Subroutines.Get(name.Value, 0)) == null)
                    throw new RantException(reader.Source, name, "No subroutine was found with the name '" + name.Value + "' and 0 parameters.");
            }
            else
            {
                args = reader.ReadItemsToClosureTrimmed(TokenType.LeftSquare, TokenType.RightSquare, TokenType.Semicolon,
                    BracketPairs.All).ToArray();
                if((sub = interpreter.Engine.Subroutines.Get(name.Value, args.Length)) == null)
                    throw new RantException(reader.Source, name, "No subroutine was found with the name '" + name.Value + "' and " + args.Length + " parameter" + (args.Length != 1 ? "s" : "") + ".");
            }

            state.AddPreBlueprint(new SubCallBlueprint(interpreter, reader.Source, sub, args));

            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoSubDefinition(Token<TokenType> first, Interpreter interpreter, PatternReader reader, State state)
        {
            bool meta = reader.Take(TokenType.Question);
            reader.Read(TokenType.LeftSquare);

            var parameters = new List<Tuple<string, ParamFlags>>();
            var tName = reader.Read(TokenType.Text, "subroutine name");

            if (!Util.ValidateName(tName.Value))
                throw new RantException(reader.Source, tName, "Invalid subroutine name: '" + tName.Value + "'");
            
            if (!reader.Take(TokenType.Colon))
            {
                reader.Read(TokenType.RightSquare);
            }
            else
            {
                while (true)
                {
                    bool isTokens = reader.Take(TokenType.At);
                    parameters.Add(Tuple.Create(reader.Read(TokenType.Text, "parameter name").Value, isTokens ? ParamFlags.Code : ParamFlags.None));
                    if (reader.Take(TokenType.RightSquare, false)) break;
                    reader.Read(TokenType.Semicolon);
                }
            }

            reader.SkipSpace();
            reader.Read(TokenType.Colon);

            var body = reader.ReadToScopeClose(TokenType.LeftSquare, TokenType.RightSquare, BracketPairs.All).ToArray();

            if (meta)
            {
                interpreter.PushState(State.CreateSub(reader.Source, body, interpreter));
                state.AddPreBlueprint(new DelegateBlueprint(interpreter, _ =>
                {
                    _.Engine.Subroutines.Define(tName.Value, Subroutine.FromString(tName.Value, _.PopResultString(), parameters.ToArray()));
                    return false;
                }));
            }
            else
            {
                interpreter.Engine.Subroutines.Define(tName.Value, Subroutine.FromTokens(tName.Value, reader.Source, body, parameters.ToArray()));
            }

            return meta;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool DoReplacer(Token<TokenType> name, Interpreter interpreter, PatternReader reader, State state)
        {
            reader.Read(TokenType.Colon);

            var args = reader.ReadItemsToClosureTrimmed(TokenType.LeftSquare, TokenType.RightSquare, TokenType.Semicolon, BracketPairs.All).ToArray();
            if (args.Length != 2) throw new RantException(reader.Source, name, "Replacer expected 2 arguments, but got " + args.Length + ".");

            state.AddPreBlueprint(new ReplacerBlueprint(interpreter, Util.ParseRegex(name.Value), args[1]));

            interpreter.PushState(State.CreateSub(reader.Source, args[0], interpreter));
            return true;
        }

        private static bool DoBlock(Interpreter interpreter, Token<TokenType> firstToken, PatternReader reader, State state)
        {
            Tuple<IEnumerable<Token<TokenType>>[], int> items;

            // Check if the block is already cached
            if (!reader.Source.TryGetCachedBlock(firstToken, out items))
            {
                var block = reader.ReadMultiItemScope(firstToken, TokenType.LeftCurly, TokenType.RightCurly, TokenType.Pipe, BracketPairs.All).ToArray();
                items = Tuple.Create(block, reader.Position);
                reader.Source.CacheBlock(firstToken, block, reader.Position);
            }
            else
            {
                // If the block is cached, seek to its end
                reader.Position = items.Item2;
            }
            
            var attribs = interpreter.NextAttribs;
            interpreter._blockAttribs = new BlockAttribs();

            if (!items.Item1.Any() || !interpreter.TakeChance()) return false;

            var rep = new Repeater(items.Item1, attribs);
            interpreter.PushRepeater(rep);
            interpreter.BaseStates.Add(state);
            state.AddPreBlueprint(new RepeaterBlueprint(interpreter, rep));
            return true;
        }
    }
}