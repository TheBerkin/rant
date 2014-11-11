using System;
using System.Collections.Generic;

using Rant.Compiler;
using Rant.Vocabulary;

namespace Rant
{
    /// <summary>
    /// The central class of the Rant engine that allows the execution of patterns.
    /// </summary>
    public sealed partial class RantEngine
    {
        /// <summary>
        /// The default NSFW filtering option to apply when creating Engine objects that load vocabulary from a directory.
        /// </summary>
        public static NsfwFilter DefaultNsfwFilter = NsfwFilter.Disallow;

        /// <summary>
        /// The indefinite article rules to apply when running patterns.
        /// </summary>
        internal static IndefiniteArticle CurrentIndefiniteArticleI = IndefiniteArticle.English;

        public static IndefiniteArticle CurrentIndefiniteArticle
        {
            get { return CurrentIndefiniteArticleI; }
            set
            {
                if (value == null) throw new ArgumentNullException("value");
                CurrentIndefiniteArticleI = value;
            }
        }

        /// <summary>
        /// The maximum stack size allowed for a pattern.
        /// </summary>
        public static int MaxStackSize = 64;

        internal readonly Dictionary<string, Query> GlobalQueryMacros = new Dictionary<string, Query>(); 

        private static readonly RNG Seeds = new RNG();

        private readonly VarStore _vars = new VarStore();
        private readonly SubStore _subs = new SubStore();
        private readonly HookCollection _hooks = new HookCollection();
        private readonly HashSet<string> _flags = new HashSet<string>();
        

        internal VarStore Variables
        {
            get { return _vars; }
        }

        internal SubStore Subroutines
        {
            get { return _subs; }
        }

        /// <summary>
        /// The currently set flags.
        /// </summary>
        public HashSet<string> Flags
        {
            get { return _flags; }
        }

        /// <summary>
        /// The hook collection associated with the engine.
        /// </summary>
        public HookCollection Hooks
        {
            get { return _hooks; }
        }

        /// <summary>
        /// Creates a new Engine object with no vocabulary.
        /// </summary>
        public RantEngine()
        {
            LoadVocab("", DefaultNsfwFilter);
        }

        /// <summary>
        /// Creates a new Engine object that loads vocabulary from the specified path.
        /// </summary>
        /// <param name="vocabularyPath">The path to the dictionary files to load.</param>
        public RantEngine(string vocabularyPath)
        {
            LoadVocab(vocabularyPath, DefaultNsfwFilter);
        }

        /// <summary>
        /// Creates a new Engine object that loads vocabulary from a path according to the specified filtering option.
        /// </summary>
        /// <param name="vocabularyPath">The path to the dictionary files to load.</param>
        /// <param name="filter">The filtering option to apply when loading the files.</param>
        public RantEngine(string vocabularyPath, NsfwFilter filter)
        {
            LoadVocab(vocabularyPath, filter);
        }

        /// <summary>
        /// Creates a new Engine object with the specified vocabulary.
        /// </summary>
        /// <param name="vocabulary">The vocabulary to load in this instance.</param>
        public RantEngine(IRantVocabulary vocabulary)
        {
            if (vocabulary == null) throw new ArgumentNullException("vocabulary");
            _vocabulary = vocabulary;
        }

        /// <summary>
        /// Compiles the specified string into a pattern, executes it, and returns the resulting output.
        /// </summary>
        /// <param name="input">The input string to execute.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(string input, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromString(input), new RNG(Seeds.NextRaw()), charLimit).Run();
        }

        /// <summary>
        /// Loads the file located at the specified path and executes it, returning the resulting output.
        /// </summary>
        /// <param name="path">The path to the file to execute.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output DoFile(string path, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromFile(path), new RNG(Seeds.NextRaw()), charLimit).Run();
        }

        /// <summary>
        /// Compiles the specified string into a pattern, executes it using a custom seed, and returns the resulting output.
        /// </summary>
        /// <param name="input">The input string to execute.</param>
        /// <param name="seed">The seed to generate output with.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(string input, long seed, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromString(input), new RNG(seed), charLimit).Run();
        }

        /// <summary>
        /// Loads the file located at the specified path and executes it using a custom seed, returning the resulting output.
        /// </summary>
        /// <param name="path">The path to the file to execute.</param>
        /// <param name="seed">The seed to generate output with.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output DoFile(string path, long seed, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromFile(path), new RNG(seed), charLimit).Run();
        }

        /// <summary>
        /// Compiles the specified string into a pattern, executes it using a custom RNG, and returns the resulting output.
        /// </summary>
        /// <param name="input">The input string to execute.</param>
        /// <param name="rng">The random number generator to use when generating output.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(string input, RNG rng, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromString(input), rng, charLimit).Run();
        }

        /// <summary>
        /// Loads the file located at the specified path and executes it using a custom seed, returning the resulting output.
        /// </summary>
        /// <param name="path">The path to the file to execute.</param>
        /// <param name="rng">The random number generator to use when generating output.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output DoFile(string path, RNG rng, int charLimit = 0)
        {
            return new Interpreter(this, RantPattern.FromFile(path), rng, charLimit).Run();
        }

        /// <summary>
        /// Executes the specified pattern and returns the resulting output.
        /// </summary>
        /// <param name="input">The pattern to execute.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(RantPattern input, int charLimit = 0)
        {
            return new Interpreter(this, input, new RNG(Seeds.NextRaw()), charLimit).Run();
        }

        /// <summary>
        /// Executes the specified pattern using a custom seed and returns the resulting output.
        /// </summary>
        /// <param name="input">The pattern to execute.</param>
        /// <param name="seed">The seed to generate output with.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(RantPattern input, long seed, int charLimit = 0)
        {
            return new Interpreter(this, input, new RNG(seed), charLimit).Run();
        }

        /// <summary>
        /// Executes the specified pattern using a custom random number generator and returns the resulting output.
        /// </summary>
        /// <param name="input">The pattern to execute.</param>
        /// <param name="rng">The random number generator to use when generating output.</param>
        /// <param name="charLimit">The maximum number of characters that can be printed. An exception will be thrown if the limit is exceeded. Set to zero or below for unlimited characters.</param>
        /// <returns></returns>
        public Output Do(RantPattern input, RNG rng, int charLimit = 0)
        {
            return new Interpreter(this, input, rng, charLimit).Run();
        }
    }
}