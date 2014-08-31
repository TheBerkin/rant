﻿using System;

using Manhood.Compiler;

using Stringes;
using Stringes.Tokens;

namespace Manhood
{
    /// <summary>
    /// Represents runtime errors raised by the Manhood engine.
    /// </summary>
    public sealed class ManhoodException : Exception
    {
        private readonly int _line;
        private readonly int _col;
        private readonly int _index;
        private readonly Source _source;
        
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
        /// The source of the error.
        /// </summary>
        public Source SourceCode { get { return _source; } }

        internal ManhoodException(Source source, Stringe token, string message = "A generic syntax error was encountered.") : base((token != null ? ("(Ln " + token.Line + ", Col " + token.Column + ") ") : "") + message)
        {
            _source = source;
            if (token != null)
            {
                _line = token.Line;
                _col = token.Column;
                _index = token.Offset;
            }
            else
            {
                _line = _col = 1;
                _index = 0;
            }
        }
    }
}