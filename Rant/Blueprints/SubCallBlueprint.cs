using System.Collections.Generic;
using System.Linq;

using Rant.Compiler;
using Rant.Stringes.Tokens;

namespace Rant.Blueprints
{
    internal class SubCallBlueprint : Blueprint
    {
        private readonly Subroutine _subroutine;
        private readonly Argument[] _args;

        public SubCallBlueprint(Interpreter interpreter, RantPattern source, Subroutine subroutine, IEnumerable<Token<TokenType>>[] args) : base(interpreter)
        {
            _subroutine = subroutine;
            if (args == null)
            {
                _args = Enumerable.Empty<Argument>().ToArray();
            }
            else
            {
                // Insert token arguments into the array, set string args to null.
                _args = args.Select((a, i) => _subroutine.Parameters[i].Item2 == ParamFlags.Code ? Argument.FromTokens(a) : null).ToArray();

                // Queue string arguments on the stack.
                for (int i = 0; i < _subroutine.ParamCount; i++)
                {
                    if (_subroutine.Parameters[i].Item2 == ParamFlags.None)
                    {
                        interpreter.PushState(Interpreter.State.CreateSub(source, args[i], interpreter));
                    }
                }
            }
        }

        public override bool Use()
        {
            // Fill in empty string arguments with state results.
            for (int i = 0; i < _args.Length; i++)
            {
                if (_args[i] == null) _args[i] = Argument.FromString(I.PopResultString());
            }

            // Create an argument table so the args can be accessed by name.
            var argTable =
                _args.Select((arg, i) => new KeyValuePair<string, Argument>(_subroutine.Parameters[i].Item1, arg)).ToDictionary(pair => pair.Key, pair => pair.Value);
            
            // Create the state for the subroutine code
            var state = new Interpreter.State(I, _subroutine.Source, I.CurrentState.Output);

            // Pre-blueprint pushes args
            state.AddPreBlueprint(new DelegateBlueprint(I, _ =>
            {
                _.SubArgStack.Push(argTable);
                return false;
            }));

            // Post-blueprint pops args
            state.AddPostBlueprint(new DelegateBlueprint(I, _ =>
            {
                _.SubArgStack.Pop();
                return false;
            }));

            // Push the state onto the stack
            I.PushState(state);

            return true;
        }
    }
}