using System;
using System.Collections.Generic;
using System.IO;
using Rant.Stringes.Tokens;

namespace Rant.Compiler
{
    /// <summary>
    /// Represents a compiled pattern that can be executed by the engine. It is recommended to use this class when running the same patern multiple times.
    /// </summary>
    public sealed class RantPattern
    {
        private readonly string _code;
        private readonly IEnumerable<Token<TokenType>> _tokens;
        private readonly SourceType _type;
        private readonly string _name;

        // This is used to cache item locations within blocks, which eliminates unnecessary multiple traversals of the blocks' tokens.
        // Item1 = Block items
        // Item2 = End position of block
        private readonly Dictionary<Token<TokenType>, Tuple<IEnumerable<Token<TokenType>>[], int>> _blockJumpTable = new Dictionary<Token<TokenType>, Tuple<IEnumerable<Token<TokenType>>[], int>>();

        /// <summary>
        /// The name of the source code.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }

        /// <summary>
        /// Describes the origin of the source.
        /// </summary>
        public SourceType Type
        {
            get { return _type; }
        }

        /// <summary>
        /// The code contained in the source.
        /// </summary>
        public string Code
        {
            get { return _code; }
        }

        internal void CacheBlock(Token<TokenType> start, IEnumerable<Token<TokenType>>[] block, int end)
        {
            _blockJumpTable[start] = Tuple.Create(block, end);
        }

        internal bool TryGetCachedBlock(Token<TokenType> start, out Tuple<IEnumerable<Token<TokenType>>[], int> block)
        {
            return _blockJumpTable.TryGetValue(start, out block);
        }

        internal IEnumerable<Token<TokenType>> Tokens
        {
            get { return _tokens; }
        }

        internal RantPattern(string name, SourceType type, string code)
        {
            _name = name;
            _type = type;
            _code = code;
            _tokens = Lexer.GenerateTokens(code);
        }

        internal RantPattern(RantPattern derived, IEnumerable<Token<TokenType>> sub)
        {
            _name = derived._name;
            _type = derived._type;
            _code = derived._code;
            _blockJumpTable = derived._blockJumpTable;

            _tokens = sub;
        }

        internal RantPattern(string name, RantPattern derived, IEnumerable<Token<TokenType>> sub)
        {
            _name = name;
            _type = derived._type;
            _code = derived._code;
            _blockJumpTable = derived._blockJumpTable;

            _tokens = sub;
        }

        /// <summary>
        /// Compiles a Source object from the specified string.
        /// </summary>
        /// <param name="code">The code to compile.</param>
        /// <returns></returns>
        public static RantPattern FromString(string code)
        {
            return new RantPattern("Source", SourceType.String, code);
        }

        /// <summary>
        /// Compiles a Source object from a string with the specified name.
        /// </summary>
        /// <param name="name">The name to give the source.</param>
        /// <param name="code">The code to compile.</param>
        /// <returns></returns>
        public static RantPattern FromString(string name, string code)
        {
            return new RantPattern(name, SourceType.String, code);
        }

        internal static RantPattern Derived(RantPattern source, IEnumerable<Token<TokenType>> tokens)
        {
            return new RantPattern(source, tokens);
        }
        
        // Used for applying a different name to subroutines
        internal static RantPattern Derived(string name, RantPattern source, IEnumerable<Token<TokenType>> tokens)
        {
            return new RantPattern(name, source, tokens);
        }

        /// <summary>
        /// Loads the file located at the specified path and creates a Source object from its contents.
        /// </summary>
        /// <param name="path">The path to the file to load.</param>
        /// <returns></returns>
        public static RantPattern FromFile(string path)
        {
            return new RantPattern(Path.GetFileName(path), SourceType.File, File.ReadAllText(path));
        }

        /// <summary>
        /// Returns a string describing the source.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Concat("(", Type, ") ", Name);
        }
    }
}