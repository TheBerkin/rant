using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Rant.Blueprints;
using Rant.Compiler;
using Rant.Stringes;

namespace Rant
{
    // The return value is returned by the blueprint that executes the tag. If it's true, the interpreter will skip to the top of the state stack.
    internal delegate bool FuncTagCallback(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args);

    internal partial class Interpreter
    {
        internal static readonly Dictionary<string, FuncDef> TagFuncs;

        static Interpreter()
        {
            TagFuncs = new Dictionary<string, FuncDef>();

            TagFuncs["rep"] = TagFuncs["r"] = new FuncSig(Repeat, ParamFlags.None);
            TagFuncs["num"] = TagFuncs["n"] = new FuncSig(Number, ParamFlags.None, ParamFlags.None);
            TagFuncs["sep"] = TagFuncs["s"] = new FuncSig(Separator, ParamFlags.Code);
            TagFuncs["before"] = new FuncSig(Before, ParamFlags.Code);
            TagFuncs["after"] = new FuncSig(After, ParamFlags.Code);
            TagFuncs["chance"] = new FuncSig(Chance, ParamFlags.None);
            TagFuncs["sync"] = new FuncSig(Sync, ParamFlags.None, ParamFlags.None);
            TagFuncs["desync"] = new FuncSig(Desync);
            TagFuncs["pin"] = new FuncSig(Pin, ParamFlags.None);
            TagFuncs["unpin"] = new FuncSig(Unpin, ParamFlags.None);
            TagFuncs["step"] = new FuncSig(Step, ParamFlags.None);
            TagFuncs["reset"] = new FuncSig(Reset, ParamFlags.None);
            TagFuncs["reseed"] = new FuncSig(Reseed, ParamFlags.None, ParamFlags.None);
            TagFuncs["first"] = new FuncSig(First, ParamFlags.Code);
            TagFuncs["last"] = new FuncSig(Last, ParamFlags.Code);
            TagFuncs["middle"] = new FuncSig(Middle, ParamFlags.Code);
            TagFuncs["notfirst"] = new FuncSig(NotFirst, ParamFlags.Code);
            TagFuncs["notlast"] = new FuncSig(NotLast, ParamFlags.Code);
            TagFuncs["notmiddle"] = new FuncSig(NotMiddle, ParamFlags.Code);
            TagFuncs["odd"] = new FuncSig(Odd, ParamFlags.Code);
            TagFuncs["even"] = new FuncSig(Even, ParamFlags.Code);
            TagFuncs["nth"] = new FuncDef(
                new FuncSig(Nth, ParamFlags.None, ParamFlags.None, ParamFlags.Code),
                new FuncSig(NthSimple, ParamFlags.None, ParamFlags.Code)
                );
            TagFuncs["repnum"] = TagFuncs["rn"] = new FuncSig(RepNum);
            TagFuncs["repindex"] = TagFuncs["ri"] = new FuncSig(RepIndex);
            TagFuncs["repcount"] = TagFuncs["rc"] = new FuncSig(RepCount);
            TagFuncs["alt"] = new FuncSig(Alt, ParamFlags.Code, ParamFlags.Code);
            TagFuncs["any"] = new FuncSig(Any, ParamFlags.Code, ParamFlags.Code);
            TagFuncs["match"] = new FuncSig(ReplaceMatch);
            TagFuncs["group"] = new FuncSig(ReplaceGroup, ParamFlags.None);
            TagFuncs["arg"] = new FuncSig(Arg, ParamFlags.None);
            TagFuncs["numfmt"] = new FuncSig(NumFmt, ParamFlags.None);
            TagFuncs["caps"] = new FuncSig(Caps, ParamFlags.None);
            TagFuncs["capsinfer"] = new FuncSig(CapsInfer, ParamFlags.None);
            TagFuncs["out"] = new FuncSig(Out, ParamFlags.None, ParamFlags.None);
            TagFuncs["close"] = new FuncSig(Close, ParamFlags.None);
            TagFuncs["extern"] = TagFuncs["ext"] = new FuncSig(Extern, ParamFlags.None, ParamFlags.Multi);
            TagFuncs["mark"] = new FuncSig(Mark, ParamFlags.None);
            TagFuncs["dist"] = new FuncSig(Dist, ParamFlags.None, ParamFlags.None);
            TagFuncs["get"] = new FuncSig(Get, ParamFlags.None);
            TagFuncs["send"] = new FuncSig(Send, ParamFlags.None, ParamFlags.None);
            TagFuncs["osend"] = new FuncSig(SendOverwrite, ParamFlags.None, ParamFlags.None);
            TagFuncs["clrt"] = new FuncSig(ClearTarget, ParamFlags.None);
            TagFuncs["len"] = new FuncSig(Length, ParamFlags.None);
            TagFuncs["char"] = new FuncDef(
                new FuncSig(Character, ParamFlags.None),
                new FuncSig(CharacterMulti, ParamFlags.None, ParamFlags.None));
            TagFuncs["define"] = new FuncSig(DefineFlag, ParamFlags.None | ParamFlags.Multi);
            TagFuncs["undef"] = new FuncSig(UndefineFlag, ParamFlags.None | ParamFlags.Multi);
            TagFuncs["ifdef"] = new FuncSig(IfDef, ParamFlags.None, ParamFlags.Code);
            TagFuncs["ifndef"] = new FuncSig(IfNDef, ParamFlags.None, ParamFlags.Code);
            TagFuncs["else"] = new FuncSig(Else, ParamFlags.Code);
            TagFuncs["cmp"] = new FuncSig(Compare, ParamFlags.None, ParamFlags.None, ParamFlags.Code);
            TagFuncs["is"] = new FuncSig(CmpIs, ParamFlags.None, ParamFlags.Code);
            TagFuncs["break"] = new FuncSig(Break);
            TagFuncs["src"] = new FuncSig(Src);
        }

        private static bool Src(Interpreter interpreter, Pattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(source.Code); // no no no no
            return false;
        }

        private static bool Break(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null) return false;
            interpreter.CurrentRepeater.Finish();
            State state = null;
            while (!interpreter.BaseStates.Contains(state = interpreter.CurrentState))
            {
                interpreter.PopState();
            }
            interpreter.BaseStates.Remove(state);
            return true;
        }

        private static bool CmpIs(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var cmp = interpreter.Comparisons.Peek();
            var conStrings = args[0].GetString()
                .Split(new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
            ComparisonResult e;
            foreach (var conString in conStrings)
            {
                if (!Enum.TryParse(Util.NameToCamel(conString), true, out e)) continue;
                if (!cmp.Result.HasFlag(e)) continue;
                interpreter.PushState(State.CreateSub(source, args[1].GetTokens(), interpreter, interpreter.CurrentState.Output));
                return true;
            }

            return false;
        }

        private static bool Compare(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var cmp = new Comparison(args[0].GetString(), args[1].GetString());
            interpreter.Comparisons.Push(cmp);

            var state = State.CreateSub(source, args[2].GetTokens(), interpreter, interpreter.CurrentState.Output);
            state.AddPostBlueprint(new DelegateBlueprint(interpreter, I =>
            {
                I.Comparisons.Pop();
                return false;
            }));

            interpreter.PushState(state);

            return true;
        }

        private static bool Reseed(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Reseed(args[0].GetString(), args[1].GetString());
            return false;
        }

        private static bool IfNDef(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (!interpreter.Engine.Flags.Contains(args[0]))
            {
                interpreter.PushState(State.CreateSub(source, args[1].GetTokens(), interpreter, interpreter.CurrentState.Output));
                return true;
            }
            interpreter.SetElse();
            return false;
        }

        private static bool Else(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (!interpreter.UseElse()) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool IfDef(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.Engine.Flags.Contains(args[0]))
            {
                interpreter.PushState(State.CreateSub(source, args[1].GetTokens(), interpreter, interpreter.CurrentState.Output));
                return true;
            }
            interpreter.SetElse();
            return false;
        }

        private static bool CharacterMulti(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            int count;
            if (!Util.ParseInt(args[1], out count) || count < 0)
                throw new RantException(source, tagname, "Invalid character count specified. Must be a non-negative number greater than zero.");
            for (int i = 0; i < count; i++)
            {
                interpreter.Print(Util.SelectFromRanges(args[0], interpreter.RNG));
            }
            return false;
        }

        private static bool Character(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(Util.SelectFromRanges(args[0], interpreter.RNG));
            return false;
        }

        private static bool UndefineFlag(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            foreach (var flag in args.Where(flag => !String.IsNullOrEmpty(flag)))
            {
                if (!Util.ValidateName(flag))
                    throw new RantException(source, tagname, "Invalid flag name '" + flag + "'");

                interpreter.Engine.Flags.Remove(flag);
            }
            return false;
        }

        private static bool DefineFlag(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            foreach (var flag in args.Where(flag => !String.IsNullOrEmpty(flag)))
            {
                if (!Util.ValidateName(flag))
                    throw new RantException(source, tagname, "Invalid flag name '" + flag + "'");

                interpreter.Engine.Flags.Add(flag);
            }
            return false;
        }

        private static bool Length(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(args[0].GetString().Length);
            return false;
        }

        private static bool Send(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.CurrentState.Output.WriteToTarget(args[0], args[1]);
            return false;
        }

        private static bool SendOverwrite(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.CurrentState.Output.WriteToTarget(args[0], args[1], true);
            return false;
        }

        private static bool Get(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.CurrentState.Output.CreateTarget(args[0]);
            return false;
        }

        private static bool ClearTarget(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.CurrentState.Output.ClearTarget(args[0]);
            return false;
        }

        private static bool Dist(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(interpreter.GetMarkerDistance(args[0], args[1]));
            return false;
        }

        private static bool Mark(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.SetMarker(args[0]);
            return false;
        }

        private static bool Extern(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var name = args[0].GetString();
            var result = interpreter.Engine.Hooks.Call(name, args.Skip(1).Select(arg => arg.GetString()).ToArray());
            if (result == null)
            {
                throw new RantException(source, tagname, "A hook with the name '" + name + "' does not exist.");
            }
            interpreter.Print(result);
            return false;
        }

        private static bool Reset(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
        {
            interpreter.Reset(args[0].GetString());
            return false;
        }

        private static bool CapsInfer(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            // TODO: Make capsinfer properly infer "first" capitalization given multiple sentences. Currently, it mistakes it for "word" mode.
            var words = Regex.Matches(args[0].GetString(), @"\w+").OfType<Match>().Select(m => m.Value).ToArray();
            int wCount = words.Length;
            int uCount = 0;
            int fwCount = 0;
            bool firstCharIsUpper = false;
            for (int i = 0; i < wCount; i++)
            {
                if (words[i].All(Char.IsUpper))
                {
                    uCount++;
                }
                if (Char.IsUpper(words[i][0]))
                {
                    fwCount++;
                    if (i == 0) firstCharIsUpper = true;
                }
            }
            if (uCount == wCount)
            {
                interpreter.CurrentState.Output.SetCaps(Capitalization.Upper);
            }
            else if (wCount > 1 && fwCount == wCount)
            {
                interpreter.CurrentState.Output.SetCaps(Capitalization.Word);
            }
            else if (firstCharIsUpper)
            {
                interpreter.CurrentState.Output.SetCaps(Capitalization.First);
            }
            return false;
        }

        private static bool Close(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.CurrentState.Output.PopChannel(args[0].GetString());
            return false;
        }

        private static bool Out(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            ChannelVisibility cv;
            var cv_str = args[1].GetString();
            if (!Enum.TryParse(Util.NameToCamel(cv_str), out cv))
            {
                throw new RantException(source, tagname, "Invalid channel visibility option '" + cv_str + "'");
            }
            interpreter.CurrentState.Output.PushChannel(args[0].GetString(), cv);
            return false;
        }

        private static bool Caps(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            Capitalization caps;
            var caps_str = args[0].GetString();
            if (!Enum.TryParse(Util.NameToCamel(caps_str), out caps))
            {
                throw new RantException(source, tagname, "Invalid capitalization format '" + caps_str + "'");
            }
            interpreter.CurrentState.Output.SetCaps(caps);
            return false;
        }

        private static bool NumFmt(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            NumberFormat fmt;
            var fmtstr = args[0].GetString();
            if (!Enum.TryParse(Util.NameToCamel(fmtstr), out fmt))
            {
                throw new RantException(source, tagname, "Invalid number format '" + fmtstr + "'");
            }
            interpreter.NumberFormat = fmt;
            return false;
        }

        private static bool Arg(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (!interpreter.SubArgStack.Any())
                throw new RantException(source, tagname, "Tried to access arguments outside of a subroutine body.");

            Argument arg;
            var argName = args[0].GetString().Trim();
            if (!interpreter.SubArgStack.Peek().TryGetValue(argName, out arg))
                throw new RantException(source, tagname, "Could not find argument '" + argName + "'.");

            // Argument is string
            if (arg.Flags == ParamFlags.None)
            {
                interpreter.Print(arg.GetString());
                return false;
            }
            
            // Argument is tokens
            interpreter.PushState(State.CreateSub(source, arg.GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool ReplaceGroup(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(interpreter.GetMatchString(args[0].GetString()));
            return false;
        }

        private static bool ReplaceMatch(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Print(interpreter.GetMatchString());
            return false;
        }

        private static bool Alt(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var testState = State.CreateSub(source, args[0].GetTokens(), interpreter,
                interpreter.CurrentState.Output);
            testState.AddPostBlueprint(new AltBlueprint(interpreter, testState, args[1].GetTokens()));
            interpreter.PushState(testState);
            return true;
        }

        private static bool Any(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var testState = State.CreateSub(source, args[0].GetTokens(), interpreter,
                interpreter.CurrentState.Output);
            testState.AddPostBlueprint(new AnyBlueprint(interpreter, testState, args[1].GetTokens()));
            interpreter.PushState(testState);
            return true;
        }

        private static bool RepCount(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null) throw new RantException(source, tagname, "No active repeater.");
            interpreter.Print(interpreter.FormatNumber(interpreter.CurrentRepeater.Count));
            return false;
        }

        private static bool RepIndex(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null) throw new RantException(source, tagname, "No active repeaters.");
            interpreter.Print(interpreter.FormatNumber(interpreter.CurrentRepeater.Index));
            return false;
        }

        private static bool RepNum(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null) throw new RantException(source, tagname, "No active repeaters.");
            interpreter.Print(interpreter.FormatNumber(interpreter.CurrentRepeater.Index + 1));
            return false;
        }

        private static bool Nth(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            int offset, interval;
            if (!Util.ParseInt(args[0].GetString(), out interval))
            {
                throw new RantException(source, tagname, "Invalid interval value.");
            }

            if (interval <= 0)
            {
                throw new RantException(source, tagname, "Interval must be greater than zero.");
            }

            if (!Util.ParseInt(args[1].GetString(), out offset))
            {
                throw new RantException(source, tagname, "Invalid offset value.");
            }

            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.Nth(offset, interval)) return false;
            interpreter.PushState(State.CreateSub(source, args[2].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool NthSimple(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            int interval;
            if (!Util.ParseInt(args[0].GetString(), out interval))
            {
                throw new RantException(source, tagname, "Invalid interval value.");
            }

            if (interval <= 0)
            {
                throw new RantException(source, tagname, "Interval must be greater than zero.");
            }

            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.Nth(0, interval)) return false;
            interpreter.PushState(State.CreateSub(source, args[1].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool Even(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.IsEven) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool Odd(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.IsOdd) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool NotMiddle(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || interpreter.CurrentRepeater.IsFirst || interpreter.CurrentRepeater.IsLast) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool NotLast(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || interpreter.CurrentRepeater.IsLast) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool NotFirst(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || interpreter.CurrentRepeater.IsFirst) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool Middle(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || interpreter.CurrentRepeater.IsLast || interpreter.CurrentRepeater.IsFirst) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool Last(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.IsLast) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool First(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            if (interpreter.CurrentRepeater == null || !interpreter.CurrentRepeater.IsFirst) return false;
            interpreter.PushState(State.CreateSub(source, args[0].GetTokens(), interpreter, interpreter.CurrentState.Output));
            return true;
        }

        private static bool Step(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Step(args[0].GetString());
            return false;
        }

        private static bool Unpin(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Unpin(args[0].GetString());
            return false;
        }

        private static bool Pin(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Pin(args[0].GetString());
            return false;
        }

        private static bool Desync(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.Desync();
            return false;
        }

        private static bool Sync(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            var typeStr = args[1].GetString();
            SyncType type;
            if (!Enum.TryParse(typeStr, true, out type))
            {
                throw new RantException(source, tagname, "Invalid synchronizer type: '" + typeStr + "'");
            }
            interpreter.Sync(args[0].GetString(), type);
            return false;
        }

        private static bool Chance(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            int a;
            if (!Util.ParseInt(args[0].GetString(), out a))
            {
                throw new RantException(source, tagname, "Invalid chance number.");
            }
            interpreter.SetChance(a);
            return false;
        }

        private static bool After(Interpreter interpreter, RantPattern source, Stringe tagname, Argument[] args)
        {
            interpreter.NextAttribs.After = args[0].GetTokens();
            return false;
        }

        private static bool Before(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
        {
            interpreter.NextAttribs.Before = args[0].GetTokens();
            return false;
        }

        private static bool Number(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
        {
            int a, b;
            if (!Util.ParseInt(args[0].GetString(), out a) || !Util.ParseInt(args[1].GetString(), out b))
            {
                throw new RantException(source, tagName, "Range values could not be parsed. They must be numbers.");
            }
            interpreter.Print(interpreter.FormatNumber(interpreter.RNG.Next(a, b + 1)));
            return false;
        }

        private static bool Separator(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
        {
            interpreter.NextAttribs.Separator = args[0].GetTokens();
            return false;
        }

        private static bool Repeat(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
        {
            var reps = args[0].GetString().Trim();
            if (String.Equals(reps, "each", StringComparison.OrdinalIgnoreCase))
            {
                interpreter.NextAttribs.Repetitons = Repeater.Each;
                return false;
            }

            int num;
            if (!Util.ParseInt(reps, out num))
            {
                throw new RantException(source, tagName, "Invalid repetition value '" + reps + "' - must be a number.");
            }
            if (num < 0)
            {
                throw new RantException(source, tagName, "Repetition value cannot be negative.");
            }

            interpreter.NextAttribs.Repetitons = num;
            return false;
        }

        /// <summary>
        /// Represents a group of related Rant function signatures.
        /// </summary>
        internal class FuncDef
        {
            private readonly IEnumerable<FuncSig> _defs; 

            public FuncDef(params FuncSig[] defs)
            {
                _defs = defs.OrderByDescending(d => d.MinArgCount).ThenBy(d => d.ParamCount);
            }

            public FuncSig GetSignature(int paramCount)
            {
                return _defs.FirstOrDefault(d => d.HasMultiFlag ? paramCount >= d.MinArgCount : paramCount == d.ParamCount);
            }

            public static implicit operator FuncDef(FuncSig def)
            {
                return new FuncDef(def);
            }
        }

        /// <summary>
        /// Represents a Rant function signature with a specific set of parameters.
        /// </summary>
        internal class FuncSig
        {
            private readonly int _paramCount;
            private readonly int _minArgCount;
            private readonly bool _hasMultiFlag;
            private readonly ParamFlags[] _parameters;
            private readonly FuncTagCallback _func;
            
            public ParamFlags[] Parameters
            {
                get { return _parameters; }
            }

            public int ParamCount
            {
                get { return _paramCount; }
            }

            public int MinArgCount
            {
                get { return _minArgCount; }
            }

            public bool HasMultiFlag
            {
                get { return _hasMultiFlag; }
            }

            public FuncSig(FuncTagCallback func, params ParamFlags[] parameters)
            {
                if (parameters.Where((t, i) => i < parameters.Length - 1 && t.HasFlag(ParamFlags.Multi)).Any())
                    throw new ArgumentException("The flag 'ParamType.Multi' is only valid on the last parameter.");

                _parameters = parameters;
                _hasMultiFlag = parameters.Any() && parameters.Last().HasFlag(ParamFlags.Multi);
                _paramCount = parameters.Length;
                _minArgCount = _hasMultiFlag ? _paramCount - 1 : _paramCount;
                _func = func;
            }

            public bool Invoke(Interpreter interpreter, RantPattern source, Stringe tagName, Argument[] args)
            {
                return _func(interpreter, source, tagName, args);
            }
        }
    }
}