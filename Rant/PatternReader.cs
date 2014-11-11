using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

using Rant.Compiler;
using Rant.Stringes.Tokens;

namespace Rant
{
    internal class PatternReader
    {
        private readonly RantPattern _source;
        private readonly Token<TokenType>[] _tokens;
        private int _pos;

        private readonly Stack<Token<TokenType>> _stack = new Stack<Token<TokenType>>(10); 

        public PatternReader(RantPattern source)
        {
            _source = source;
            _tokens = source.Tokens.ToArray();
            _pos = 0;
        }

        public RantPattern Source
        {
            get { return _source; }
        }

        public int Position
        {
            get { return _pos; }
            set { _pos = value; }
        }

        public bool End
        {
            get { return _pos >= _tokens.Length; }
        }

        public Token<TokenType> PrevToken
        {
            get { return _pos == 0 ? null : _tokens[_pos - 1]; }
        }
            
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token<TokenType> ReadToken()
        {
            if (End) throw new RantException(_source, null, "Unexpected end of file.");
            return _tokens[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token<TokenType> PeekToken()
        {
            if (End) throw new RantException(_source, null, "Unexpected end of file.");
            return _tokens[_pos];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNext(TokenType type)
        {
            return !End && _tokens[_pos].Identifier == type;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Take(TokenType type, bool allowEof = true)
        {
            if (End)
            {
                if (!allowEof)
                    throw new RantException(_source, null, "Unexpected end-of-file.");
                return false;
            }
            if (_tokens[_pos].Identifier != type) return false;
            _pos++;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TakeAll(TokenType type, bool allowEof = true)
        {
            if (End)
            {
                if (!allowEof)
                    throw new RantException(_source, null, "Unexpected end-of-file.");
                return false;
            }
            if (_tokens[_pos].Identifier != type) return false;
            do
            {
                _pos++;
            } while (!End && _tokens[_pos].Identifier == type);
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Token<TokenType> Read(TokenType type, string expectedTokenName = null)
        {
            if (End) 
                throw new RantException(_source, null, "Expected " + (expectedTokenName ?? "'" + Lexer.Rules.GetSymbolForId(type) + "'") + ", but hit end of file.");
            if (_tokens[_pos].Identifier != type)
            {
                throw new RantException(_source, _tokens[_pos], "Expected " + (expectedTokenName ?? "'" + Lexer.Rules.GetSymbolForId(type) + "'"));
            }
            return _tokens[_pos++];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TakeAllUntil(TokenType takeType, TokenType untilType, bool allowEof = true)
        {
            if (End)
            {
                if (!allowEof)
                    throw new RantException(_source, null, "Unexpected end-of-file.");
                return false;
            }

            int i = _pos;
            TokenType t;
            while (i < _tokens.Length)
            {
                t = _tokens[_pos].Identifier;
                if (t == takeType)
                {
                    i++;
                }
                else if (t == untilType)
                {
                    _pos = i;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool SkipSpace()
        {
            return TakeAll(TokenType.Whitespace);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<IEnumerable<Token<TokenType>>> ReadMultiItemScope(Token<TokenType> first, TokenType open, TokenType close, TokenType separator, BracketPairs bracketPairs)
        {
            _stack.Clear();
            _stack.Push(first);

            int start = _pos;
            Token<TokenType> token = null;

            while (!End)
            {
                // Peek but don't consume - this saves some calculations later on.
                token = PeekToken();

                // Opening bracket
                if (bracketPairs.ContainsOpening(token.Identifier) || open == token.Identifier) // Previous bracket allows nesting
                {
                    _stack.Push(token);
                }

                // Closing bracket
                else if (bracketPairs.ContainsClosing(token.Identifier) || close == token.Identifier) // Previous bracket allows nesting
                {
                    var lastOpening = _stack.Pop();

                    // Handle invalid closures
                    if (!bracketPairs.Contains(lastOpening.Identifier, token.Identifier)) // Not in main pair
                    {
                        throw new RantException(_source, token, 
                            "Invalid closure '"
                            + lastOpening.Value
                            + " ... " 
                            + token.Value 
                            + "' - expected '" 
                            + Lexer.Rules.GetSymbolForId(bracketPairs.GetClosing(lastOpening.Identifier)) 
                            + "'");
                    }

                    // If the _stack is empty, this is the last item. Stop the iterator.
                    if (!_stack.Any())
                    {
                        yield return _tokens.SkipWhile((t, i) => i < start).TakeWhile((t, i) => i < _pos - start).ToArray();
                        _pos++;
                        yield break;
                    }
                }

                // Separator
                else if (token.Identifier == separator && _stack.Count == 1)
                {
                    yield return _tokens.SkipWhile((t, i) => i < start).TakeWhile((t, i) => i < _pos - start).ToArray();
                    start = _pos + 1;
                }

                // Move to next position
                _pos++;
            }
            throw new RantException(_source, null, "Unexpected end of file - expected '" + Lexer.Rules.GetSymbolForId(close) + "'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<IEnumerable<Token<TokenType>>> ReadItemsToClosureTrimmed(TokenType open, TokenType close, TokenType separator, BracketPairs bracketPairs)
        {
            SkipSpace();
            _stack.Clear();
            Token<TokenType> token = null;

            int start = _pos;
            while (!End)
            {
                token = PeekToken();
                if (bracketPairs.ContainsOpening(token.Identifier) || open == token.Identifier) // Allows nesting
                {
                    _stack.Push(token);
                }
                else if (bracketPairs.ContainsClosing(token.Identifier) || token.Identifier == close) // Allows nesting
                {
                    // Since this method assumes that the first opening bracket was already read, an empty _stack indicates main scope closure.
                    if (!_stack.Any() && token.Identifier == close)
                    {
                        yield return _tokens.SkipWhile((t, i) => i < start).TakeWhile((t, i) => i < _pos - start).ToArray();
                        _pos++;
                        yield break;
                    }

                    var lastOpening = _stack.Pop();
                    if (!bracketPairs.Contains(lastOpening.Identifier, token.Identifier))
                    {
                        throw new RantException(_source, token,
                            "Invalid closure '"
                            + lastOpening.Value
                            + " ... "
                            + token.Value
                            + "' - expected '"
                            + Lexer.Rules.GetSymbolForId(bracketPairs.GetClosing(lastOpening.Identifier))
                            + "'");
                    }
                }
                else if (token.Identifier == separator && !_stack.Any())
                {
                    yield return _tokens.SkipWhile((t, i) => i < start).TakeWhile((t, i) => i < _pos - start).ToArray();
                    _pos++;
                    TakeAll(TokenType.Whitespace);
                    start = _pos;
                    continue;
                }

                _pos++;
            }
            throw new RantException(_source, null, "Unexpected end of file - expected '" + Lexer.Rules.GetSymbolForId(close) + "'.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IEnumerable<Token<TokenType>> ReadToScopeClose(TokenType open, TokenType close, BracketPairs bracketPairs)
        {
            SkipSpace();
            _stack.Clear();
            Token<TokenType> token = null;
            while (!End)
            {
                token = ReadToken();
                if (bracketPairs.ContainsOpening(token.Identifier) || open == token.Identifier) // Allows nesting
                {
                    _stack.Push(token);
                }
                else if (bracketPairs.ContainsClosing(token.Identifier) || token.Identifier == close) // Allows nesting
                {
                    // Since this method assumes that the first opening bracket was already read, an empty _stack indicates main scope closure.
                    if (!_stack.Any() && token.Identifier == close)
                    {
                        yield break;
                    }

                    var lastOpening = _stack.Pop();
                    
                    if (!bracketPairs.Contains(lastOpening.Identifier, token.Identifier))
                    {
                        throw new RantException(_source, token,
                            "Invalid closure '"
                            + lastOpening.Value
                            + " ... "
                            + token.Value
                            + "' - expected '"
                            + Lexer.Rules.GetSymbolForId(bracketPairs.GetClosing(lastOpening.Identifier))
                            + "'");
                    }
                }
                yield return token;
            }
            throw new RantException(_source, null, "Unexpected end of file - expected '" + Lexer.Rules.GetSymbolForId(close) + "'.");
        }
    }
}