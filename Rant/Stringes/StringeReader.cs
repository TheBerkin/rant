using System;
using System.Linq;
using System.Text.RegularExpressions;
using Rant.Stringes.Tokens;

namespace Rant.Stringes
{
    /// <summary>
    /// Represents a reader that can read data from a stringe.
    /// </summary>
    internal sealed class StringeReader
    {
        private readonly Stringe _stringe;
        private int _pos;

        /// <summary>
        /// Creates a new StringeReader instance using the specified string as input.
        /// </summary>
        /// <param name="value">The string to use as input. This will be converted to a root-level stringe.</param>
        public StringeReader(string value)
        {
            _stringe = value.ToStringe();
            _pos = 0;
        }

        /// <summary>
        /// Creates a new StringeReader instance using the specified stringe as input.
        /// </summary>
        /// <param name="value">The stringe to use as input.</param>
        public StringeReader(Stringe value)
        {
            _stringe = value;
            _pos = 0;
        }

        /// <summary>
        /// Indicates whether the reader position is at the end of the input string.
        /// </summary>
        public bool EndOfStringe
        {
            get { return _pos >= _stringe.Length; }
        }

        /// <summary>
        /// Reads a charactere from the input and advances the position by one.
        /// </summary>
        /// <returns></returns>
        public Chare ReadChare()
        {
            return _stringe[_pos++];
        }

        /// <summary>
        /// Returns the next charactere in the input, but does not consume it.
        /// </summary>
        /// <returns></returns>
        public Chare PeekChare()
        {
            return EndOfStringe ? null : _stringe[_pos];
        }

        /// <summary>
        /// Reads a stringe from the input and advances the position by the number of characters read.
        /// </summary>
        /// <param name="length">The number of characters to read.</param>
        /// <returns></returns>
        public Stringe ReadStringe(int length)
        {
            int p = _pos;
            _pos += length;
            return _stringe.Substringe(p, length);
        }

        /// <summary>
        /// Indicates whether the specified character occurs at the reader's current position, and consumes it.
        /// </summary>
        /// <param name="value">The character to test for.</param>
        /// <returns></returns>
        public bool Eat(char value)
        {
            if (PeekChare() != value) return false;
            _pos++;
            return true;
        }

        /// <summary>
        /// Indicates whether the specified string occurs at the reader's current position, and consumes it.
        /// </summary>
        /// <param name="value">The string to test for.</param>
        /// <returns></returns>
        public bool Eat(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;
            if (_stringe.IndexOf(value, _pos) != _pos) return false;
            _pos += value.Length;
            return true;
        }

        /// <summary>
        /// Indicates whether the specified regular expression matches the input at the reader's current position. If a match is found, the reader consumes it.
        /// </summary>
        /// <param name="regex">The regular expression to test for.</param>
        /// <returns></returns>
        public bool Eat(Regex regex)
        {
            if (regex == null) throw new ArgumentNullException("regex");
            var match = regex.Match(_stringe.Value, _pos);
            if (!match.Success || match.Index != _pos) return false;
            _pos += match.Length;
            return true;
        }

        /// <summary>
        /// Indicates whether the specified regular expression matches the input at the reader's current position. If a match is found, the reader consumes it and outputs the result.
        /// </summary>
        /// <param name="regex">The regular expression to test for.</param>
        /// <param name="result">The stringe to output the result to.</param>
        /// <returns></returns>
        public bool Eat(Regex regex, out Stringe result)
        {
            if (regex == null) throw new ArgumentNullException("regex");
            result = null;
            var match = regex.Match(_stringe.Value, _pos);
            if (!match.Success || match.Index != _pos) return false;
            result = _stringe.Substringe(_pos, match.Length);
            _pos += match.Length;
            return true;
        }

        /// <summary>
        /// Indicates whether the specified character occurs at the reader's current position.
        /// </summary>
        /// <param name="value">The character to test for.</param>
        /// <returns></returns>
        public bool IsNext(char value)
        {
            return PeekChare() == value;
        }

        /// <summary>
        /// Indicates whether the specified string occurs at the reader's current position.
        /// </summary>
        /// <param name="value">The string to test for.</param>
        /// <returns></returns>
        public bool IsNext(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;
            return _stringe.IndexOf(value, _pos) == _pos;
        }

        /// <summary>
        /// Indicates whether the specified regular expression matches the input at the reader's current position.
        /// </summary>
        /// <param name="regex">The regular expression to test for.</param>
        /// <returns></returns>
        public bool IsNext(Regex regex)
        {
            if (regex == null) throw new ArgumentNullException("regex");
            var match = regex.Match(_stringe.Value, _pos);
            return match.Success && match.Index == _pos;
        }

        /// <summary>
        /// Indicates whether the specified regular expression matches the input at the reader's current position, and outputs the result.
        /// </summary>
        /// <param name="regex">The regular expression to test for.</param>
        /// <param name="result">The stringe to output the result to.</param>
        /// <returns></returns>
        public bool IsNext(Regex regex, out Stringe result)
        {
            if (regex == null) throw new ArgumentNullException("regex");
            result = null;
            var match = regex.Match(_stringe.Value, _pos);
            if (!match.Success || match.Index != _pos) return false;
            result = _stringe.Substringe(_pos, match.Length);
            return true;
        }

        /// <summary>
        /// Advances the reader position past any immediate white space characters.
        /// </summary>
        public void SkipWhiteSpace()
        {
            while (!EndOfStringe && Char.IsWhiteSpace(_stringe.Value[_pos]))
            {
                _pos++;
            }
        }

        /// <summary>
        /// Reads the next token from the current position, then advances the position past it.
        /// </summary>
        /// <typeparam name="T">The token identifier type to use.</typeparam>
        /// <param name="rules">The lexer rules to use.</param>
        /// <returns></returns>
        public Token<T> ReadToken<T>(LexerRules<T> rules) where T : struct
        {
            readStart:

            if (EndOfStringe)
            {
                if (rules.EndToken != null && !rules.IgnoreRules.Contains(rules.EndToken.Item2))
                {
                    return new Token<T>(rules.EndToken.Item2, _stringe.Substringe(_pos, 0));
                }

                throw new InvalidOperationException("Unexpected end of input.");
            }

            // Indicates if undefined tokens should be created
            bool captureUndef = rules.UndefinedCaptureRule != null;

            // Tracks the beginning of the undefined token content
            int u = _pos;

            do
            {
                // If we've reached the end, return undefined token, if present.
                if (EndOfStringe && captureUndef && u < _pos)
                {
                    if (rules.IgnoreRules.Contains(rules.UndefinedCaptureRule.Item2)) goto readStart;
                    return new Token<T>(rules.UndefinedCaptureRule.Item2, rules.UndefinedCaptureRule.Item1(_stringe.Slice(u, _pos)));
                }

                // Check high priority symbol rules
                foreach (var t in rules.HighSymbols.Where(t => IsNext(t.Item1)))
                {
                    // Return undefined token if present
                    if (captureUndef && u < _pos)
                    {
                        if (rules.IgnoreRules.Contains(rules.UndefinedCaptureRule.Item2)) goto readStart;
                        return new Token<T>(rules.UndefinedCaptureRule.Item2, rules.UndefinedCaptureRule.Item1(_stringe.Slice(u, _pos)));
                    }

                    // Return symbol token
                    var c = _stringe.Substringe(_pos, t.Item1.Length);
                    _pos += t.Item1.Length;
                    if (rules.IgnoreRules.Contains(t.Item2)) goto readStart;
                    return new Token<T>(t.Item2, c);
                }
                
                const string tokenGroupName = "value";

                // Check regex rules
                if (rules.RegexList.Any())
                {
                    Match longestMatch = null;
                    var id = default(T);

                    // Find the longest match, if any.
                    foreach (var re in rules.RegexList)
                    {
                        var match = re.Item1.Match(_stringe.Value, _pos);
                        if (match.Success && match.Index == _pos && (longestMatch == null || match.Length > longestMatch.Length))
                        {
                            longestMatch = match;
                            id = re.Item2.GetValue(match);
                        }
                    }

                    // If there was a match, generate a token.
                    if (longestMatch != null)
                    {
                        // Return undefined token if present
                        if (captureUndef && u < _pos)
                        {
                            if (rules.IgnoreRules.Contains(rules.UndefinedCaptureRule.Item2)) goto readStart;
                            return new Token<T>(rules.UndefinedCaptureRule.Item2, rules.UndefinedCaptureRule.Item1(_stringe.Slice(u, _pos)));
                        }

                        // Return longest match, narrow down to <value> group if available.
                        var group = longestMatch.Groups[tokenGroupName];
                        if (group.Success)
                        {
                            _pos = group.Index + group.Length;
                            if (rules.IgnoreRules.Contains(id)) goto readStart;
                            return new Token<T>(id, _stringe.Substringe(group.Index, group.Length));
                        }

                        _pos += longestMatch.Length;
                        if (rules.IgnoreRules.Contains(id)) goto readStart;
                        return new Token<T>(id, _stringe.Substringe(longestMatch.Index, longestMatch.Length));
                    }
                }

                // Check normal priority symbol rules
                foreach (var t in rules.NormalSymbols.Where(t => IsNext(t.Item1)))
                {
                    // Return undefined token if present
                    if (captureUndef && u < _pos)
                    {
                        if (rules.IgnoreRules.Contains(rules.UndefinedCaptureRule.Item2)) goto readStart;
                        return new Token<T>(rules.UndefinedCaptureRule.Item2, rules.UndefinedCaptureRule.Item1(_stringe.Slice(u, _pos)));
                    }

                    // Return symbol token
                    var c = _stringe.Substringe(_pos, t.Item1.Length);
                    _pos += t.Item1.Length;
                    if (rules.IgnoreRules.Contains(t.Item2)) goto readStart;
                    return new Token<T>(t.Item2, c);
                }

                _pos++;

                if(!captureUndef)
                {
                    var bad = _stringe.Slice(u, _pos);
                    throw new InvalidOperationException(String.Concat("(Ln ", bad.Line, ", Col ", bad.Column, ") Invalid token '", bad, "'"));
                }

            } while (captureUndef);

            throw new InvalidOperationException("This should never happen.");
        }

        /// <summary>
        /// The current zero-based position of the reader.
        /// </summary>
        public int Position
        {
            get { return _pos; }
            set
            {
                if (value < 0 || value > _stringe.Length)
                {
                    throw new ArgumentOutOfRangeException("value");
                }
                _pos = value;
            }
        }

        /// <summary>
        /// The total length, in characters, of the stringe being read.
        /// </summary>
        public int Length
        {
            get { return _stringe.Length; }
        }
    }
}