using System;

using Rant.Compiler;
using Rant.Stringes;

namespace Rant
{
    /// <summary>
    /// Represents a runtime error raised by the Rant engine.
    /// </summary>
    public sealed class RantException : Exception
    {
        private readonly int _line;
        private readonly int _col;
        private readonly int _index;
        private readonly string _source;
        private readonly int _length;

        /// <summary>
        /// The line on which the error occurred.
        /// </summary>
        public int Line { get { return _line; } }

        /// <summary>
        /// The column on which the error occurred.
        /// </summary>
        public int Column { get { return _col; } }

        /// <summary>
        /// The character index on which the error occurred.
        /// </summary>
        public int Index { get { return _index; } }

        /// <summary>
        /// The length of the substring in which the error occurred.
        /// </summary>
        public int Length { get { return _length; } }

        /// <summary>
        /// The source of the error.
        /// </summary>
        public string Code { get { return _source; } }

        internal RantException(RantPattern source, Stringe token, string message = "A generic syntax error was encountered.") : base((token != null ? ("(" + source.Name + " @ Ln " + token.Line + ", Col " + token.Column + ") - ") : "") + message)
        {
            _source = source.Code;
            if (token != null)
            {
                _line = token.Line;
                _col = token.Column;
                _index = token.Offset;
                _length = token.Length;
            }
            else
            {
                _line = _col = 1;
                _index = 0;
                _length = 0;
            }
        }

        internal RantException(string source, Stringe token, string message = "A generic syntax error was encountered.")
            : base((token != null ? ("(Ln " + token.Line + ", Col " + token.Column + ") - ") : "") + message)
        {
            _source = source;
            if (token != null)
            {
                _line = token.Line;
                _col = token.Column;
                _index = token.Offset;
                _length = token.Length;
            }
            else
            {
                _line = _col = 1;
                _index = 0;
                _length = 0;
            }
        }
    }
}