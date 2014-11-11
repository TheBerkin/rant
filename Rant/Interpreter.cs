using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

using Rant.Compiler;
using Rant.Stringes.Tokens;
using Rant.Vocabulary;

namespace Rant
{
    internal partial class Interpreter
    {
        // Main fields
        private readonly RNG _rng;
        private readonly RantPattern _mainSource;
        private readonly RantEngine _engine;

        // Queries
        private readonly CarrierSyncState _carrierSyncState = new CarrierSyncState();
        private readonly Dictionary<string, Query> _localQueryMacros = new Dictionary<string, Query>(); 

        // Next block attribs
        private BlockAttribs _blockAttribs = new BlockAttribs();

        // The chance value of the next block
        private int _chance = 100;

        // Number format
        private NumberFormat _numfmt = NumberFormat.Normal;
        
        // State information
        private int _stateCount = 0;
        private State _prevState = null;
        private readonly State _mainState;

        // Flag conditional fields
        private bool _else = false;

        // Stacks
        private readonly Stack<State> _stateStack = new Stack<State>();
        private readonly Stack<Output> _resultStack = new Stack<Output>();
        private readonly Stack<Repeater> _repeaterStack = new Stack<Repeater>();
        private readonly Stack<Match> _matchStack = new Stack<Match>();
        private readonly Stack<Dictionary<string, Argument>> _subArgStack = new Stack<Dictionary<string, Argument>>();
        private readonly Stack<Comparison> _comparisons = new Stack<Comparison>(); 
        private readonly ChannelStack _output;

        private readonly HashSet<State> _baseStates = new HashSet<State>(); 

        private readonly Limit<int> _charLimit;

        public CarrierSyncState CarrierSyncState
        {
            get { return _carrierSyncState; }
        }

        public Dictionary<string, Query> LocalQueryMacros
        {
            get { return _localQueryMacros; }
        }

        public RantEngine Engine
        {
            get { return _engine; }
        }

        public Limit<int> CharLimit
        {
            get { return _charLimit; }
        }

        public HashSet<State> BaseStates
        {
            get { return _baseStates; }
        }

        public BlockAttribs NextAttribs
        {
            get { return _blockAttribs; }
            set { _blockAttribs = value; }
        }

        public RNG RNG
        {
            get { return _rng; }
        }

        public Stack<Dictionary<string, Argument>> SubArgStack
        {
            get { return _subArgStack; }
        }

        public Stack<Comparison> Comparisons
        {
            get { return _comparisons; }
        }

        public NumberFormat NumberFormat
        {
            get { return _numfmt; }
            set { _numfmt = value; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string FormatNumber(double value)
        {
            return Numerals.FormatNumber(value, _numfmt);
        }

        #region Flag conditionals
        public void SetElse()
        {
            _else = true;
        }

        public bool UseElse()
        {
            bool e = _else;
            _else = false;
            return e;
        }
        #endregion

        #region Repeaters

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushRepeater(Repeater repeater)
        {
            _repeaterStack.Push(repeater);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Repeater PopRepeater()
        {
            return _repeaterStack.Pop();
        }

        public Repeater CurrentRepeater
        {
            get { return _repeaterStack.Any() ? _repeaterStack.Peek() : null; }
        }

        #endregion

        #region Chance

        public void SetChance(int chance)
        {
            _chance = chance < 0 ? 0 : chance > 100 ? 100 : chance;
        }

        public bool TakeChance()
        {
            if (_chance == 100) return true;
            bool pass = _rng.Next(100) < _chance;
            _chance = 100;
            return pass;
        }

        #endregion

        #region Replacers

        public string GetMatchString(string group = null)
        {
            if (!_matchStack.Any()) return "";
            return !String.IsNullOrEmpty(@group) ? _matchStack.Peek().Groups[@group].Value : _matchStack.Peek().Value;
        }

        public void PushMatch(Match match)
        {
            _matchStack.Push(match);
        }

        public Match PopMatch()
        {
            return _matchStack.Pop();
        }

        #endregion

        #region States

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void PushState(State state)
        {
            if (_stateCount >= RantEngine.MaxStackSize)
            {
                throw new RantException(_mainSource, null, "Exceeded maximum stack size of " + RantEngine.MaxStackSize + ".");
            }
            if (_stateStack.Any()) _prevState = _stateStack.Peek();
            _stateCount++;
            _stateStack.Push(state);
        }

        public State PrevState
        {
            get { return _prevState; }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public State PopState()
        {
            _stateCount--;
            var s = _stateStack.Pop();
            _prevState = _stateCount > 0 ? _stateStack.Peek() : null;
            return s;
        }

        public State CurrentState
        {
            get { return _stateStack.Any() ? _stateStack.Peek() : null; }
        }

        #endregion
        
        public Interpreter(RantEngine engine, RantPattern input, RNG rng, int limitChars = 0)
        {
            _mainSource = input;
            _rng = rng;
            _engine = engine;
            _charLimit = new Limit<int>(0, limitChars, (a, b) => a + b, (a, b) => b == 0 || a <= b);
            _output = new ChannelStack(_charLimit);
            _mainState = new State(this, input, _output);
        }

        public void Print(object input)
        {
            _stateStack.Peek().Print(input.ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string PopResultString()
        {
            return !_resultStack.Any() ? "" : _resultStack.Pop().MainValue;
        }

        public Output Run()
        {
            PushState(_mainState);

            // ReSharper disable TooWideLocalVariableScope

            State state;                // The current state object being used
            PatternReader reader;        // The current source reader being used
            Token<TokenType> token;     // The next token in the stream

            // ReSharper restore TooWideLocalVariableScope

            next:
            while (_stateCount > 0)
            {
                // Because blueprints can sometimes queue more blueprints, loop until the topmost state does not have one.
                do
                {
                    state = CurrentState;
                } while (state.UsePreBlueprint());
                
                reader = state.Reader;

                while (!reader.End)
                {
                    // Fetch the next token in the stream without consuming it
                    token = reader.ReadToken();
                    
                    // Error on illegal closure
                    if (BracketPairs.All.ContainsClosing(token.Identifier))
                    {
                        throw new RantException(reader.Source, token, "Unexpected token '" + Lexer.Rules.GetSymbolForId(token.Identifier) + "'");
                    }

                    // DoElement will return true if the interpreter should skip to the top of the stack
                    if (DoElement(token, reader, state))
                    {
                        goto next;
                    }
                }

                // Use post-blueprints
                while (state.UsePostBlueprint()) { }

                // Push a result string if the state's output differs from the one below it
                if (!state.SharesOutput && state.Finish())
                {
                    _resultStack.Push(state.Output.GetOutput());
                }

                // Remove state from stack as long as nothing else was added beforehand
                if (state == CurrentState)
                {
                    PopState();
                    BaseStates.Remove(state);
                }
            }

            return _resultStack.Pop();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool DoElement(Token<TokenType> token, PatternReader reader, State state)
        {
            TokenFunc func;
            if (TokenFuncs.TryGetValue(token.Identifier, out func)) return func(this, token, reader, state);
            Print(token.Value);
            return false;
        }
    }
}