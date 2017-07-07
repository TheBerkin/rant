#region License

// https://github.com/TheBerkin/Rant
// 
// Copyright (c) 2017 Nicholas Fleck
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in the
// Software without restriction, including without limitation the rights to use, copy,
// modify, merge, publish, distribute, sublicense, and/or sell copies of the Software,
// and to permit persons to whom the Software is furnished to do so, subject to the
// following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A
// PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF
// CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE
// OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

#endregion

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using Rant.Core.Compiler.Syntax;
using Rant.Core.Constructs;
using Rant.Core.Formatting;
using Rant.Core.ObjectModel;
using Rant.Core.Output;
using Rant.Core.Utilities;
using Rant.Metadata;
using Rant.Vocabulary.Querying;
using Rant.Vocabulary.Utilities;
// ReSharper disable UnusedParameter.Local

// ReSharper disable UnusedMember.Local

namespace Rant.Core.Framework
{
	// Methods representing Rant functions must be marked with [RantFunction] attribute to get registered by the engine.
	// They may return either void or IEnumerator<RantAction> depending on your needs.
	internal static partial class RantFunctionRegistry
	{
		#region Subroutines

		[RantFunction]
		[RantDescription("Returns the specified argument from the current subroutine.")]
		private static IEnumerator<RST> Arg(Sandbox sb,
			[RantDescription("The name of the argument to retrieve.")] string name)
		{
			if (!sb.SubroutineArgs.Any()) yield break;
			var args = sb.SubroutineArgs.Peek();
			if (args.ContainsKey(name))
				yield return args[name];
		}

		#endregion

		#region Channels

		[RantFunction]
		[RantDescription("Opens a channel for writing and executes the specified pattern inside of it.")]
		private static IEnumerator<RST> Chan(Sandbox sb,
						     [RantDescription("The name of the channel.")] string channelName, ChannelVisibility visibility, RST pattern)
		{
			sb.Output.OpenChannel(channelName, visibility);
			yield return pattern;
			sb.Output.CloseChannel();
		}

		#endregion

		#region Dependencies

		[RantFunction("require")]
		[RantDescription("Loads and runs a pattern from cache or file.")]
		private static IEnumerator<RST> Require(Sandbox sb,
			[RantDescription("The name or path of the pattern to load.")] string name)
		{
			RST action;

			try
			{
				action = sb.Engine.GetProgramInternal(name).SyntaxTree;
			}
			catch (RantCompilerException e)
			{
				throw new RantRuntimeException(sb, sb.CurrentAction.Location,
					$"Failed to compile imported pattern '{name}':\n{e.Message}");
			}
			catch (Exception e)
			{
				throw new RantRuntimeException(sb, sb.CurrentAction.Location,
					$"Failed to import '{name}':\n{e.Message}");
			}

			yield return action;
		}

		#endregion

		#region Serial

		[RantFunction]
		[RantDescription("Yields the currenty written output.")]
		private static void Yield(Sandbox sb) => sb.SetYield();

		#endregion

		#region Input

		[RantFunction("in")]
		[RantDescription("Prints the value of the specified pattern argument.")]
		private static void PatternArg(Sandbox sb,
			[RantDescription("The name of the argument to access.")] string argName)
		{
			if (sb.PatternArgs == null) return;
			sb.Output.Print(sb.PatternArgs[argName]);
		}

		#endregion

		#region Conditions

		[RantFunction("switch")]
		[RantDescription("Tests input for equality against the case pairs.")]
		private static IEnumerator<RST> Switch(Sandbox sb, 
						       [RantDescription("The string for which to find.")] string input,
						       [RantDescription("The case pairs against which to test the input.")] params RST[] casePairs)
		{
			if (casePairs.Length % 2 != 0)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-switch-incomplete-pair");

			for (int i = 0; i < casePairs.Length; i += 2)
			{
				sb.AddOutputWriter();
				yield return casePairs[i];
				string output = sb.Return().Main;
				if (String.Equals(input, output, StringComparison.Ordinal))
				{
					yield return casePairs[i + 1];
					yield break;
				}
			}
		}

		#endregion

		#region Numbers and Number Formatting

		[RantFunction("num", "n")]
		[RantDescription("Prints a random number between the specified minimum and maximum bounds.")]
		private static void Number(Sandbox sb,
			[RantDescription("The minimum value of the number to generate.")] int min,
			[RantDescription("The maximum value of the number to generate.")] int max) => sb.Print(sb.RNG.Next(min, max + 1));

		[RantFunction("num")]
		[RantDescription("Formats an input string using the current number format settings and prints the result.")]
		private static void Number(Sandbox sb,
			[RantDescription("The string to convert into a number.")] string input)
		{
			sb.Print(Util.ParseDouble(input, out double number) ? number : 0);
		}

		[RantFunction("numfmt")]
		[RantDescription("Sets the current number formatting mode.")]
		private static void NumberFormat(Sandbox sb,
			[RantDescription("The number format to use.")] NumberFormat format) => sb.Output.Do(chain => chain.Last.NumberFormatter.NumberFormat = format);

		[RantFunction("numfmt")]
		[RantDescription("Runs the specified pattern under a specific number formatting mode.")]
		private static IEnumerator<RST> NumberFormatRange(Sandbox sb,
			[RantDescription("The number format to use.")] NumberFormat format,
			[RantDescription("The pattern to run.")] RST rangeAction)
		{
			var oldFmtMap = new Dictionary<OutputChain, NumberFormat>();

			sb.Output.Do(chain =>
			{
				oldFmtMap[chain] = chain.Last.NumberFormatter.NumberFormat;
				chain.Last.NumberFormatter.NumberFormat = format;
			});

			yield return rangeAction;

			sb.Output.Do(chain =>
			{
				if (!oldFmtMap.TryGetValue(chain, out NumberFormat fmt)) return;
				chain.Last.NumberFormatter.NumberFormat = fmt;
			});
		}

		[RantFunction]
		[RantDescription("Specifies the current digit formatting mode for numbers.")]
		private static void Digits(Sandbox sb,
			[RantDescription("The digit format to use.")] BinaryFormat format,
			[RantDescription("The digit count to associate with the mode.")] int digits) => sb.Output.Do(chain =>
		{
			chain.Last.NumberFormatter.BinaryFormat = format;
			chain.Last.NumberFormatter.BinaryFormatDigits = digits;
		});

		[RantFunction]
		[RantDescription("Sets the current endianness for hex and binary formatted numbers.")]
		private static void Endian(Sandbox sb,
			[RantDescription("The endianness to use.")] Endianness endianness) => sb.Output.Do(chain => chain.Last.NumberFormatter.Endianness = endianness);

		#endregion

		#region Block Attributes

		// TODO: Finish [persist].
		//[RantFunction]
		[RantDescription("Instructs Rant not to consume the block attributes after they are used.")]
		private static void Persist(Sandbox sb, AttribPersistence persistence) => sb.AttribManager.CurrentAttribs.Persistence = persistence;

		[RantFunction("pipe")]
		[RantDescription("Redirects the output from the next block into the specified callback. Access block output with [item].")]
		private static void Redirect(Sandbox sb,
			[RantDescription("The callback to redirect block output to.")] RST redirectCallback)
		{
			sb.AttribManager.CurrentAttribs.Redirect = redirectCallback;
		}

		[RantFunction("item")]
		[RantDescription("Prints the main output from the current block iteration.")]
		private static void RedirectedItem(Sandbox sb)
		{
			sb.Print(sb.GetRedirectedOutput().Main);
		}

		[RantFunction("item")]
		[RantDescription("Prints the specified channel from the current block iteration.")]
		private static void RedirectedItem(Sandbox sb,
			[RantDescription("The output channel to print from.")] string channel)
		{
			sb.Print(sb.GetRedirectedOutput()[channel]);
		}

		[RantFunction("protect")]
		[RantDescription("Spawns a new block attribute context for the specified callback so any blocks therein will not consume the current attributes.")]
		private static IEnumerator<RST> Protect(Sandbox sb,
			[RantDescription("The callback to protect.")] RST pattern)
		{
			sb.AttribManager.AddLayer();
			yield return pattern;
			sb.AttribManager.RemoveLayer();
		}

		[RantFunction]
		[RantDescription("Sets a pattern that will run before the next block.")]
		private static void Start(Sandbox sb,
			[RantDescription("The pattern to run before the next block.")] RST beforePattern) => sb.AttribManager.CurrentAttribs.Start = beforePattern;

		[RantFunction]
		[RantDescription("Sets a pattern that will run after the next block.")]
		private static void End(Sandbox sb,
			[RantDescription("The pattern to run after the next block.")] RST endPattern) => sb.AttribManager.CurrentAttribs.End = endPattern;

		[RantFunction("sync", "x")]
		[RantDescription("Creates and applies a synchronizer with the specified name and type.")]
		private static void Sync(Sandbox sb,
			[RantDescription("The name of the synchronizer.")] string name,
			[RantDescription("The synchronization type to use.")] SyncType type) => sb.SyncManager.Create(name, type, true);

		[RantFunction("init")]
		[RantDescription("Sets the index of the element to execute on the next block. Set to -1 to disable.")]
		private static void Initial(Sandbox sb, int index) => sb.AttribManager.CurrentAttribs.StartIndex = index;

		[RantFunction("rep", "r")]
		[RantDescription("Sets the repetition count for the next block.")]
		private static void Rep(Sandbox sb,
			[RantDescription("The number of times to repeat the next block.")] int times) => sb.AttribManager.CurrentAttribs.Repetitions = times;

		[RantFunction]
		[RantDescription("Sets the repetition count to the number of items in the next block.")]
		private static void RepEach(Sandbox sb) => sb.AttribManager.CurrentAttribs.RepEach = true;

		[RantFunction("sep")]
		private static IEnumerator<RST> PrintSep(Sandbox sb)
		{
			yield return sb.Blocks.Peek().Attribs.Separator;
		}

		[RantFunction("sep", "s")]
		[RantDescription("Sets the separator pattern for the next block.")]
		private static void Sep(Sandbox sb,
			[RantDescription("The separator pattern to run between iterations of the next block.")] RST separator)
		{
			sb.AttribManager.CurrentAttribs.IsSeries = false;
			sb.AttribManager.CurrentAttribs.Separator = separator;
		}

		[RantFunction("sep", "s")]
		[RantDescription("Flags the next block as a series and sets the separator and conjunction patterns.")]
		private static void Sep(Sandbox sb,
			[RantDescription("The separator pattern to run between items.")] RST separator,
			[RantDescription("The conjunction pattern to run before the last item.")] RST conjunction)
		{
			sb.AttribManager.CurrentAttribs.IsSeries = true;
			sb.AttribManager.CurrentAttribs.Separator = separator;
			sb.AttribManager.CurrentAttribs.EndConjunction = conjunction;
		}

		[RantFunction("sep", "s")]
		[RantDescription("Sets the separator, Oxford comma, and conjunction patterns for the next series.")]
		private static void Sep(Sandbox sb,
			[RantDescription("The separator pattern to run between items.")] RST separator,
			[RantDescription("The Oxford comma pattern to run before the last item.")] RST oxford,
			[RantDescription("The conjunction pattern to run before the last item in the series.")] RST conjunction)
		{
			sb.AttribManager.CurrentAttribs.IsSeries = true;
			sb.AttribManager.CurrentAttribs.Separator = separator;
			sb.AttribManager.CurrentAttribs.EndSeparator = oxford;
			sb.AttribManager.CurrentAttribs.EndConjunction = conjunction;
		}

		[RantFunction("rs")]
		[RantDescription("Sets the repetitions and separator for the next block. A combination of rep and sep.")]
		private static void RepSep(Sandbox sb,
			[RantDescription("The number of times to repeat the next block.")] int times,
			[RantDescription("The separator pattern to run between iterations of the next block.")] RST separator)
		{
			sb.AttribManager.CurrentAttribs.IsSeries = false;
			sb.AttribManager.CurrentAttribs.Repetitions = times;
			sb.AttribManager.CurrentAttribs.Separator = separator;
		}

		[RantFunction]
		[RantDescription("Sets the prefix pattern for the next block.")]
		private static void Before(Sandbox sb,
			[RantDescription("The pattern to run before each iteration of the next block.")] RST beforeAction) => sb.AttribManager.CurrentAttribs.Before = beforeAction;

		[RantFunction]
		[RantDescription("Sets the postfix pattern for the next block.")]
		private static void After(Sandbox sb,
			[RantDescription("The pattern to run after each iteration of the next block.")] RST afterAction) => sb.AttribManager.CurrentAttribs.After = afterAction;

		[RantFunction]
		[RantDescription("Modifies the likelihood that the next block will execute. Specified in percentage.")]
		private static void Chance(Sandbox sb,
			[RantDescription("The percent probability that the next block will execute.")] double chance) => sb.AttribManager.CurrentAttribs.Chance = chance < 0 ? 0 : chance > 100 ? 100 : chance;

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is the first.")]
		private static IEnumerator<RST> First(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration == 1) yield return action;
		}

		

		#endregion

		#region Text Formatting and Analysis

		[RantFunction("abbr")]
		[RantDescription("Abbreviates the specified string.")]
		private static void Abbreviate(Sandbox sb, 
			[RantDescription("The string to abbreviate.")]
			string value)
		{
			var words = value.Split(new[] { ' ', '-', '/', '.' }, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length < 3)
			{
				sb.Print(words.Aggregate((c, n) => c + Char.ToUpperInvariant(n[0]).ToString()));
				return;
			}
			var buffer = new StringBuilder();
			for(int i = 0; i < words.Length; i++)
			{
				if (i > 0 && sb.Engine.Format.Excludes(words[i])) continue;
				if (words[i].All(Char.IsDigit))
				{
					buffer.Append(words[i]);
					continue;
				}
				buffer.Append(Char.ToUpperInvariant(words[i][0]));
			}
			sb.Print(buffer.ToString());
		}

		[RantFunction("at")]
		[RantDescription("Prints the character at the specified position in the input. Throws an exception if the position is outside of the string.")]
		private static void At(Sandbox sb,
			[RantDescription("The input string.")] string input,
			[RantDescription("The position of the character to find.")] int pos)
		{
			if (pos < 0 || pos > input.Length)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-invalid-string-index", pos);
			sb.Print(input.Substring(pos, 1));
		}

		[RantFunction("len")]
		[RantDescription("Gets the length of the specified string.")]
		private static void StringLength(Sandbox sb, 
			[RantDescription("The string to measure.")]
			string str)
		{
			sb.Print(str.Length);
		}

		[RantFunction("chlen")]
		[RantDescription("Prints the current length of the specified channel, in characters.")]
		private static void ChannelLength(Sandbox sb,
			[RantDescription("The channel for which to retrieve the length.")] string channelName) => sb.Print(sb.Output.GetChannelLength(channelName));

		[RantFunction("rev")]
		[RantDescription("Reverses the specified string and prints it to the output.")]
		private static void Reverse(Sandbox sb,
			[RantDescription("The string to reverse.")] string input)
		{
			if (string.IsNullOrEmpty(input)) return;
			var buffer = new char[input.Length];
			int numCombiners = 0;
			int lastIndex = input.Length - 1;
			for (int i = lastIndex; i >= 0; i--)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(input[i]) == UnicodeCategory.NonSpacingMark)
				{
					// It's combining, so increase the combiner count until we hit a regular char
					numCombiners++;
				}
				else if (numCombiners > 0)
				{
					// We've hit a non-combining character with combiners added.
					// First thing to do is add the character to the buffer.
					buffer[lastIndex - i - numCombiners] = input[i];

					// Then we insert all the combining characters that come after it.
					for (int j = 1; j <= numCombiners; j++)
						buffer[lastIndex - i - numCombiners + j] = input[i + j];
					numCombiners = 0;
				}
				else if (char.IsLowSurrogate(input[i]))
					buffer[lastIndex - i + 1] = input[i];
				else if (char.IsHighSurrogate(input[i]))
					buffer[lastIndex - i - 1] = input[i];
				else
					buffer[lastIndex - i] = input[i];
			}
			sb.Print(new string(buffer));
		}

		[RantFunction("revx")]
		[RantDescription("Reverses the specified string and inverts common brackets and quotation marks, then prints the result to the output.")]
		private static void ReverseEx(Sandbox sb,
			[RantDescription("The string to reverse.")] string input)
		{
			if (string.IsNullOrEmpty(input)) return;
			var buffer = new char[input.Length];
			int numCombiners = 0;
			int lastIndex = input.Length - 1;
			for (int i = lastIndex; i >= 0; i--)
			{
				if (CharUnicodeInfo.GetUnicodeCategory(input[i]) == UnicodeCategory.NonSpacingMark)
				{
					// It's combining, so increase the combiner count until we hit a regular char
					numCombiners++;
				}
				else if (numCombiners > 0)
				{
					// We've hit a non-combining character with combiners added.
					// First thing to do is add the character to the buffer.
					buffer[lastIndex - i - numCombiners] = Util.ReverseChar(input[i]);

					// Then we insert all the combining characters that come after it.
					for (int j = 1; j <= numCombiners; j++)
						buffer[lastIndex - i - numCombiners + j] = input[i + j];
					numCombiners = 0;
				}
				else if (char.IsLowSurrogate(input[i]))
					buffer[lastIndex - i + 1] = input[i];
				else if (char.IsHighSurrogate(input[i]))
					buffer[lastIndex - i - 1] = input[i];
				else
					buffer[lastIndex - i] = Util.ReverseChar(input[i]);
			}
			sb.Print(new string(buffer));
		}

		[RantFunction("plural", "pl")]
		[RantDescription("Infers and prints the plural form of the specified word.")]
		private static void Plural(Sandbox sb, string word) => sb.Print(sb.Format.Pluralizer.Pluralize(word));

		[RantFunction("quote", "quot")]
		[RantDescription(
			"Surrounds the specified pattern in quotes. Nested quotes use the secondary quotes defined in the format settings.")]
		private static IEnumerator<RST> Quote(Sandbox sb,
			[RantDescription("The pattern to run whose output will be surrounded in quotes.")] RST quoteAction)
		{
			sb.IncreaseQuote();
			sb.PrintOpeningQuote();
			yield return quoteAction;
			sb.PrintClosingQuote();
			sb.DecreaseQuote();
		}

		[RantFunction]
		[RantDescription("Sets the current rhyming mode for queries.")]
		private static void Rhyme(Sandbox sb,
			[RantDescription("The rhyme types to use.")] RhymeFlags flags) => sb.CarrierState.Rhymer.AllowedRhymes = flags;

		[RantFunction("case", "caps")]
		[RantDescription("Changes the capitalization mode for all open channels.")]
		private static void Case(Sandbox sb,
			[RantDescription("The capitalization mode to use.")] Capitalization mode) => sb.Output.Capitalize(mode);

		[RantFunction("txtfmt")]
		[RantDescription("Sets the text conversion format for all open channels.")]
		private static void TxtFmt(Sandbox sb,
			[RantDescription("The conversion mode to use.")] CharConversion format) => sb.Output.SetConversion(format);

		[RantFunction]
		[RantDescription("Infers the capitalization of a given string and sets the capitalization mode to match it.")]
		private static void CapsInfer(Sandbox sb,
			[RantDescription("A string that is capitalized in the format to be set.")] string sample)
		{
			var output = sb.Output;
			if (string.IsNullOrEmpty(sample))
			{
				output.Capitalize(Capitalization.None);
				return;
			}
			var words = sample.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			if (words.Length == 1)
			{
				string word = words[0];
				if (word.Length == 1)
					output.Capitalize(char.IsUpper(word[0]) ? Capitalization.First : Capitalization.None);
				else if (Util.IsUppercase(word))
					output.Capitalize(Capitalization.Upper);
				else if (char.IsUpper(word.SkipWhile(c => !char.IsLetterOrDigit(c)).FirstOrDefault()))
					output.Capitalize(Capitalization.First);
				else
					output.Capitalize(Capitalization.None);
			}
			else
			{
				// No letters? Forget it.
				if (!sample.Any(char.IsLetter))
				{
					output.Capitalize(Capitalization.None);
					return;
				}

				// Is all-caps?
				if (Util.IsUppercase(sample))
				{
					output.Capitalize(Capitalization.Upper);
					return;
				}

				var sentences = sample.Split(new[] { '.', '?', '!' }, StringSplitOptions.RemoveEmptyEntries)
					.Select(str => str.Trim())
					.Where(str =>
						!string.IsNullOrEmpty(str)
						&& !char.IsDigit(str[0]))
					.ToArray();

				// All words capitalized?
				var lwords = words.Where(w => char.IsLetter(w[0])).ToArray();
				if (lwords.Any() &&
					(sentences.Length == 1 ||
					sentences.Any(s => s.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Length > 1)))
				{
					if (lwords.All(lw => char.IsUpper(lw[0])))
					{
						output.Capitalize(Capitalization.Word);
						return;
					}

					if (lwords.All(lw => char.IsLower(lw[0]) == sb.Format.Excludes(lw)))
					{
						output.Capitalize(Capitalization.Title);
						return;
					}
				}

				// All sentences capitalized?
				bool all = true;
				bool none = true;
				foreach (string sentence in sentences)
				{
					bool isCapitalized = char.IsUpper(sentence.SkipWhile(c => !char.IsLetter(c)).FirstOrDefault());
					all = all && isCapitalized;
					none = none && !isCapitalized;
				}

				if (sentences.Length > 1 && all)
					output.Capitalize(Capitalization.Sentence);
				else if (none)
					output.Capitalize(Capitalization.Lower);
				else if (char.IsUpper(sample.SkipWhile(c => !char.IsLetterOrDigit(c)).FirstOrDefault()))
					output.Capitalize(Capitalization.First);
				else
					output.Capitalize(Capitalization.None);
			}
		}

		#endregion

		#region Block State

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is a multiple of the specified number.")]
		private static IEnumerator<RST> Nth(Sandbox sb,
			[RantDescription("The interval at which the pattern should be run.")] int interval,
			[RantDescription("The pattern to run when the condition is satisfied.")] RST pattern)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration % interval != 0) yield break;
			yield return pattern;
		}

		[RantFunction]
		[RantDescription(
			"Runs a pattern if the current block iteration is a multiple of the specified number offset by a specific amount.")]
		private static IEnumerator<RST> NthO(Sandbox sb,
			[RantDescription("The interval at which the pattern should be run.")] int interval,
			[RantDescription("The number of iterations to offset the interval by.")] int offset,
			[RantDescription("The pattern to run when the condition is satisfied.")] RST pattern)
		{
			if (!sb.Blocks.Any()) yield break;
			if (Util.Mod(sb.Blocks.Peek().Iteration - offset, interval) != 0) yield break;
			yield return pattern;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is not a multiple of the specified number.")]
		private static IEnumerator<RST> NotNth(Sandbox sb,
			[RantDescription("The interval at which the pattern should not be run.")] int interval,
			[RantDescription("The pattern to run when the condition is satisfied.")] RST pattern)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration % interval == 0) yield break;
			yield return pattern;
		}

		[RantFunction]
		[RantDescription(
			"Runs a pattern if the current block iteration is not a multiple of the specified number offset by a specific amount."
		)]
		private static IEnumerator<RST> NotNthO(Sandbox sb,
			[RantDescription("The interval at which the pattern should not be run.")] int interval,
			[RantDescription("The number of iterations to offset the interval by.")] int offset,
			[RantDescription("The pattern to run when the condition is satisfied.")] RST pattern)
		{
			if (!sb.Blocks.Any()) yield break;
			if (Util.Mod(sb.Blocks.Peek().Iteration - offset, interval) == 0) yield break;
			yield return pattern;
		}

		[RantFunction("repnum", "rn")]
		[RantDescription("Prints the iteration number of the current block.")]
		private static void RepNum(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			sb.Print(sb.Blocks.Peek().Iteration);
		}

		[RantFunction("repelapsed", "re")]
		[RantDescription("Prints the number of iterations remaining to be performed on the current block.")]
		private static void RepElapsed(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			sb.Print(sb.Blocks.Peek().Iteration - 1);
		}

		[RantFunction("repcount", "rc")]
		[RantDescription("Prints the repetition count of the current block.")]
		private static void RepCount(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			sb.Print(sb.Blocks.Peek().Repetitions);
		}

		[RantFunction("reprem", "rr")]
		[RantDescription("Prints the number of remaining repetitions queued after the current one.")]
		private static void RepRem(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			var block = sb.Blocks.Peek();
			sb.Print(block.Repetitions - block.Iteration);
		}

		[RantFunction("repqueued", "rq")]
		[RantDescription("Prints the number of repetitions remaining to be completed on the current block.")]
		private static void RepQueued(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			var block = sb.Blocks.Peek();
			sb.Print(block.Repetitions - (block.Iteration - 1));
		}

		[RantFunction]
		[RantDescription("Prints the number of currently active blocks.")]
		private static void Depth(Sandbox sb) => sb.Print(sb.Blocks.Count);

		[RantFunction("index", "i")]
		[RantDescription("Prints the zero-based index of the block item currently being executed.")]
		private static void Index(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			sb.Print(sb.Blocks.Peek().Index);
		}

		[RantFunction("index1", "i1")]
		[RantDescription("Prints the one-based index of the block item currently being executed.")]
		private static void IndexOne(Sandbox sb)
		{
			if (!sb.Blocks.Any()) return;
			sb.Print(sb.Blocks.Peek().Index + 1);
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is an odd number.")]
		private static IEnumerator<RST> Odd(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration % 2 != 0) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is an even number.")]
		private static IEnumerator<RST> Even(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration % 2 == 0) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is not the first.")]
		private static IEnumerator<RST> NotFirst(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			if (sb.Blocks.Peek().Iteration > 1) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is the last.")]
		private static IEnumerator<RST> Last(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			var block = sb.Blocks.Peek();
			if (block.Iteration == block.Repetitions) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is not the last.")]
		private static IEnumerator<RST> NotLast(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			var block = sb.Blocks.Peek();
			if (block.Iteration < block.Repetitions) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is neither the first nor last.")]
		private static IEnumerator<RST> Middle(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			var block = sb.Blocks.Peek();
			if (block.Iteration > 1 && block.Iteration < block.Repetitions) yield return action;
		}

		[RantFunction]
		[RantDescription("Runs a pattern if the current block iteration is either the first or last.")]
		private static IEnumerator<RST> Ends(Sandbox sb,
			[RantDescription("The pattern to run when the condition is met.")] RST action)
		{
			if (!sb.Blocks.Any()) yield break;
			var block = sb.Blocks.Peek();
			if (block.Iteration == 1 || block.Iteration == block.Repetitions) yield return action;
		}

		#endregion

		#region Replacer

		[RantFunction]
		[RantDescription("Retrieves and prints the current match string of the active replacer.")]
		private static void Match(Sandbox sb)
		{
			if (!sb.RegexMatches.Any()) return;
			sb.Print(sb.RegexMatches.Peek().Value);
		}

		[RantFunction]
		[RantDescription("Retrieves and prints the specified group value of the current match from the active replacer.")]
		private static void Group(Sandbox sb,
			[RantDescription("The name of the match group whose value will be retrieved.")] string groupName)
		{
			if (!sb.RegexMatches.Any()) return;
			sb.Print(sb.RegexMatches.Peek().Groups[groupName].Value);
		}

		#endregion

		#region Synchronizer

		[RantFunction("xdel")]
		[RantDescription("Deletes a synchronizer.")]
		private static void SyncDelete(Sandbox sb, 
			[RantDescription("The name of the synchronizer to delete.")] string name) => sb.SyncManager.Delete(name);

		[RantFunction("xpin")]
		[RantDescription("Pins a synchronizer.")]
		private static void SyncPin(Sandbox sb,
			[RantDescription("The name of the synchronizer to pin.")] string name) => sb.SyncManager.SetPinned(name, true);

		[RantFunction("xunpin")]
		[RantDescription("Unpins a synchronizer.")]
		private static void SyncUnpin(Sandbox sb,
			[RantDescription("The name of the synchronizer to unpin.")] string name) => sb.SyncManager.SetPinned(name, false);

		[RantFunction("xstep")]
		[RantDescription("Iterates a synchronizer.")]
		private static void SyncStep(Sandbox sb,
			[RantDescription("The name of the synchronizer to iterate.")] string name) => sb.SyncManager.Step(name);

		[RantFunction("xreset")]
		[RantDescription("Resets a synchronizer to its initial state.")]
		private static void SyncReset(Sandbox sb,
			[RantDescription("The name of the synchronizer to reset.")] string name) => sb.SyncManager.Reset(name);

		#endregion

		#region Targets

		[RantFunction("target", "t")]
		[RantDescription("Places a target with the specified name at the current write position.")]
		private static void Target(Sandbox sb,
			[RantDescription("The name of the target.")] string targetName) => sb.Output.InsertTarget(targetName);

		[RantFunction]
		[RantDescription("Appends a string to the specified target's contents.")]
		private static void Send(Sandbox sb,
			[RantDescription("The name of the target to which to send.")] string targetName,
			[RantDescription("The string to send to the target.")] string value) => sb.Output.PrintToTarget(targetName, value);

		[RantFunction]
		[RantDescription("Overwrites the specified target's contents with the provided value.")]
		private static void SendOver(Sandbox sb,
			[RantDescription("The name of the target to to which to send.")] string targetName,
			[RantDescription("The string to send to the target.")] string value)
		{
			sb.Output.Do(chain => chain.ClearTarget(targetName));
			sb.Output.PrintToTarget(targetName, value);
		}

		[RantFunction("targetval")]
		[RantDescription("Prints the current value of the specified target. This function will not spawn a target.")]
		private static void GetTargetValue(Sandbox sb,
			[RantDescription("The name of the target whose value to print.")] string targetName) => sb.Output.Do(chain => chain.Print(chain.GetTargetValue(targetName)));

		[RantFunction("clrt")]
		[RantDescription("Clears the contents of the specified target.")]
		private static void ClearTarget(Sandbox sb,
			[RantDescription("The name of the target to be cleared.")] string targetName) => sb.Output.Do(chain => chain.ClearTarget(targetName));

		#endregion

		#region Flags

		[RantFunction]
		[RantDescription("Defines the specified flags.")]
		private static void Define(Sandbox sb,
			[RantDescription("The list of flags to define.")] params string[] flags)
		{
			foreach (string flag in flags.Where(f => !Util.IsNullOrWhiteSpace(f) && Util.ValidateName(f)))
				sb.Engine.Flags.Add(flag);
		}

		[RantFunction]
		[RantDescription("Undefines the specified flags.")]
		private static void Undef(Sandbox sb,
			[RantDescription("The list of flags to undefine.")] params string[] flags)
		{
			foreach (string flag in flags) sb.Engine.Flags.Remove(flag);
		}

		[RantFunction]
		[RantDescription("Toggles the specified flags.")]
		private static void Toggle(Sandbox sb, params string[] flags)
		{
			foreach (string flag in flags.Where(f => !Util.IsNullOrWhiteSpace(f) && Util.ValidateName(f)))
			{
				if (sb.Engine.Flags.Contains(flag))
					sb.Engine.Flags.Remove(flag);
				else
					sb.Engine.Flags.Add(flag);
			}
		}

		[RantFunction]
		[RantDescription(
			"Sets the current flag condition for [then] ... [else] calls to be true if all the specified flags are set.")]
		private static void IfDef(Sandbox sb, params string[] flags)
		{
			sb.FlagConditionExpectedResult = true;
			sb.ConditionFlags.Clear();
			foreach (string flag in flags.Where(f => !Util.IsNullOrWhiteSpace(f) && Util.ValidateName(f)))
				sb.ConditionFlags.Add(flag);
		}

		[RantFunction]
		[RantDescription(
			"Sets the current flag condition for [then] ... [else] calls to be true if all the specified flags are unset.")]
		private static void IfNDef(Sandbox sb, params string[] flags)
		{
			sb.FlagConditionExpectedResult = false;
			sb.ConditionFlags.Clear();
			foreach (string flag in flags.Where(f => !Util.IsNullOrWhiteSpace(f) && Util.ValidateName(f)))
				sb.ConditionFlags.Add(flag);
		}

		[RantFunction]
		[RantDescription("Executes a pattern if the current flag condition passes.")]
		private static IEnumerator<RST> Then(Sandbox sb, RST conditionPassPattern)
		{
			if (sb.Engine.Flags.All(flag => sb.ConditionFlags.Contains(flag) == sb.FlagConditionExpectedResult))
				yield return conditionPassPattern;
		}

		[RantFunction]
		[RantDescription("Executes a pattern if the current flag condition fails.")]
		private static IEnumerator<RST> Else(Sandbox sb, RST conditionFailPattern)
		{
			if (sb.Engine.Flags.Any(flag => sb.ConditionFlags.Contains(flag) != sb.FlagConditionExpectedResult))
				yield return conditionFailPattern;
		}

		#endregion

		#region RNG

		[RantFunction]
		[RantDescription("Branches the internal RNG according to a seed.")]
		private static void Branch(Sandbox sb,
			[RantDescription("The seed for the branch.")] string seed) => sb.RNG.Branch(seed.Hash());

		[RantFunction]
		[RantDescription("Branches the internal RNG, executes the specified pattern, and then merges the branch.")]
		private static IEnumerator<RST> Branch(Sandbox sb,
			[RantDescription("The seed for the branch.")] string seed,
			[RantDescription("The pattern to run on the branch.")] RST branchAction)
		{
			sb.RNG.Branch(seed.Hash());
			yield return branchAction;
			sb.RNG.Merge();
		}

		[RantFunction]
		[RantDescription("Merges the topmost branch of the internal RNG, if it has been branched at least once.")]
		private static void Merge(Sandbox sb) => sb.RNG.Merge();

		#endregion

		#region Characters, Accents, and Special Prints

		[RantFunction("accent")]
		[RantDescription("Accents the previous character.")]
		private static void AddAccent(Sandbox sb, Accent accent) => sb.Print(accent.GetAccentChar());

		[RantFunction("accent")]
		[RantDescription("Accents the specified character.")]
		private static void AddAccent(Sandbox sb, string character, Accent accent) => sb.Print($"{character}{accent.GetAccentChar()}".Normalize(NormalizationForm.FormC));

		[RantFunction("acute", "act")]
		[RantDescription("Accents the specified character with an acute (a\u0301) accent.")]
		private static void AccentAcute(Sandbox sb, string character) => sb.Print($"{character}\u0301".Normalize(NormalizationForm.FormC));

		[RantFunction("circumflex", "cflex")]
		[RantDescription("Accents the specified character with a circumflex (a\u0302) accent.")]
		private static void AccentCircumflex(Sandbox sb, string character) => sb.Print($"{character}\u0302".Normalize(NormalizationForm.FormC));

		[RantFunction("grave", "grv")]
		[RantDescription("Accents the specified character with a grave (a\u0300) accent.")]
		private static void AccentGrave(Sandbox sb, string character) => sb.Print($"{character}\u0300".Normalize(NormalizationForm.FormC));

		[RantFunction("ring")]
		[RantDescription("Accents the specified character with a ring (a\u030A) accent.")]
		private static void AccentRing(Sandbox sb, string character) => sb.Print($"{character}\u030A".Normalize(NormalizationForm.FormC));

		[RantFunction("tilde", "tld")]
		[RantDescription("Accents the specified character with a tilde (a\u0303) accent.")]
		private static void AccentTilde(Sandbox sb, string character) => sb.Print($"{character}\u0303".Normalize(NormalizationForm.FormC));

		[RantFunction("diaeresis", "dia")]
		[RantDescription("Accents the specified character with a diaeresis (a\u0308) accent.")]
		private static void AccentDiaeresis(Sandbox sb, string character) => sb.Print($"{character}\u0308".Normalize(NormalizationForm.FormC));

		[RantFunction("caron", "crn")]
		[RantDescription("Accents the specified character with a caron (c\u030C) accent.")]
		private static void AccentCaron(Sandbox sb, string character) => sb.Print($"{character}\u030C".Normalize(NormalizationForm.FormC));

		[RantFunction("macron", "mcn")]
		[RantDescription("Accents the specified character with a macron (c\u0304) accent.")]
		private static void AccentMacron(Sandbox sb, string character) => sb.Print($"{character}\u0304".Normalize(NormalizationForm.FormC));

		[RantFunction("cedilla", "ced")]
		[RantDescription("Accents the specified character with a cedilla (c\u0327) accent.")]
		private static void AccentCedilla(Sandbox sb, string character) => sb.Print($"{character}\u0327".Normalize(NormalizationForm.FormC));

		[RantFunction("char")]
		[RantDescription("Prints a Unicode character given its official Unicode-designated name (e.g. 'LATIN CAPITAL LETTER R' -> 'R').")]
		private static void Character(Sandbox sb,
			[RantDescription("The name of the character to print (case-insensitive).")] string name) => sb.Print(Unicode.GetByName(name));

		[RantFunction("tm")]
		[RantDescription("Prints the trademark symbol.")]
		private static void Trademark(Sandbox sb) => sb.Print("\x2122");

		[RantFunction("reg")]
		[RantDescription("Prints the registered trademark symbol.")]
		private static void RegisteredTrademark(Sandbox sb) => sb.Print("\x00ae");

		[RantFunction("c")]
		[RantDescription("Prints the copyright symbol.")]
		private static void Copyright(Sandbox sb) => sb.Print("\x00a9");

		[RantFunction("em")]
		[RantDescription("Prints an emdash.")]
		private static void Emdash(Sandbox sb) => sb.Print("\x2014");

		[RantFunction("en")]
		[RantDescription("Prints an endash.")]
		private static void Endash(Sandbox sb) => sb.Print("\x2013");

		[RantFunction("b")]
		[RantDescription("Prints a bullet character.")]
		private static void Bullet(Sandbox sb) => sb.Print("\x2022");

		[RantFunction("ss")]
		[RantDescription("Prints an eszett (ß).")]
		private static void Eszett(Sandbox sb) => sb.Print("\x00df");

		[RantFunction("emoji")]
		[RantDescription("Takes an emoji shortcode and prints the corresponding emoji.")]
		private static void PrintEmoji(Sandbox sb,
			[RantDescription("The emoji shortcode to use, without colons.")] string shortcode)
		{
			shortcode = shortcode.ToLower();
			if (!Emoji.Shortcodes.ContainsKey(shortcode))
			{
				sb.Print("[missing emoji]");
				return;
			}
			sb.Print(char.ConvertFromUtf32(Emoji.Shortcodes[shortcode]));
		}

		#endregion

		#region Query Building

		[RantFunction("rcc")]
		[RantDescription("Resets the specified carrier components.")]
		private static void ResetCarrier(Sandbox sb,
			[RantDescription("The list of carrier component identifiers to reset.")] params string[] ids)
		{
			foreach (string id in ids)
			{
				if (Util.IsNullOrWhiteSpace(id)) continue;
				sb.CarrierState.DeleteAssociation(id);
				sb.CarrierState.DeleteMatch(id);
				sb.CarrierState.DeleteRhyme(id);
				sb.CarrierState.DeleteUnique(id);
			}
		}

		[RantFunction("query", "q")]
		[RantDescription("Runs the last-accessed constructed query.")]
		private static IEnumerator<RST> QueryRun(Sandbox sb)
		{
			return sb.QueryBuilder.CurrentQuery?.Run(sb);
		}

		[RantFunction("query", "q")]
		[RantDescription("Runs the constructed query with the specified identifier.")]
		private static IEnumerator<RST> QueryRun(Sandbox sb, 
			[RantDescription("The ID string for the constructed query.")]
			string id)
		{
			return sb.QueryBuilder.RunQuery(sb, id);
		}

		[RantFunction("qname")]
		[RantDescription("Sets the table name for a constructed query.")]
		private static void QueryName(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The name of the table.")]
			string name)
		{
			sb.QueryBuilder.GetQuery(id).Name = name;
		}

		[RantFunction("qsub")]
		[RantDescription("Sets the subtype for a constructed query.")]
		private static void QuerySubtype(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The subtype of the term to select from the returned entry.")]
			string subtype)
		{
			sb.QueryBuilder.GetQuery(id).Subtype = subtype;
		}

		[RantFunction("qsubp")]
		[RantDescription("Sets the plural subtype for a constructed query.")]
		private static void QueryPluralSubtype(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The subtype of the term to select from the returned entry, if the plural flag is set.")]
			string pluralSubtype)
		{
			sb.QueryBuilder.GetQuery(id).PluralSubtype = pluralSubtype;
		}

		[RantFunction("qcf")]
		[RantDescription("Adds positive class filters to a constructed query.")]
		private static void QueryClassFilterPositive(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The names of the classes that the returned entry must belong to.")]
			params string[] classes)
		{
			Query q;
			ClassFilter cf;
			cf = (q = sb.QueryBuilder.GetQuery(id)).GetNonClassFilters().FirstOrDefault(f => f is ClassFilter) as ClassFilter;
			if (cf == null) q.AddFilter(cf = new ClassFilter());
			foreach (string cl in classes)
			{
				cf.AddRule(new ClassFilterRule(cl, true));
			}
		}

		[RantFunction("qcfn")]
		[RantDescription("Adds negative class filters to a constructed query.")]
		private static void QueryClassFilterNegative(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The names of the classes that the returned entry must not belong to.")]
			params string[] classes)
		{
			Query q;
			var cf = (q = sb.QueryBuilder.GetQuery(id)).GetNonClassFilters().FirstOrDefault(f => f is ClassFilter) as ClassFilter;
			if (cf == null) q.AddFilter(cf = new ClassFilter());
			foreach (string cl in classes)
			{
				cf.AddRule(new ClassFilterRule(cl, false));
			}
		}

		[RantFunction("qcc")]
		[RantDescription("Adds a carrier component to a constructed query.")]
		private static void QueryCarrierComponent(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The ID to assign to the carrier component.")]
			string componentId, 
			[RantDescription("The component type.")]
			CarrierComponentType componentType)
		{
			if (!Util.ValidateName(componentId)) throw new RantRuntimeException(sb, sb.CurrentAction, "err-invalid-ccid", componentId);
			var q = sb.QueryBuilder.GetQuery(id);
			(q.Carrier ?? (q.Carrier = new Carrier())).AddComponent(componentType, componentId);
		}

		[RantFunction("qdel")]
		[RantDescription("Removes all stored data associated with the specified constructed query ID.")]
		private static void QueryDelete(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id)
		{
			sb.QueryBuilder.ResetQuery(id);
		}

		[RantFunction("qexists")]
		[RantDescription("Prints a boolean value indicating whether a constructed query with the specified ID exists.")]
		private static void QueryExists(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id)
		{
			sb.Print(sb.QueryBuilder.QueryExists(id) ? TRUE : FALSE);
		}

		[RantFunction("qhas")]
		[RantDescription("Adds a positive regex filter to a constructed query.")]
		private static void QueryRegexFilter(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The regex pattern for the filter.")]
			string regexPattern, 
			[RantDescription("The regex option string for the filter.")]
			string options)
		{
			Regex regex;
			try
			{
				regex = new Regex(regexPattern, Util.GetRegexOptionsFromString(options));
			}
			catch (Exception ex)
			{
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-invalid-regex", ex.Message);
			}
			sb.QueryBuilder.GetQuery(id).AddFilter(new RegexFilter(regex, true));
		}

		[RantFunction("qhasno")]
		[RantDescription("Adds a positive regex filter to a constructed query.")]
		private static void QueryNegativeRegexFilter(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id,
			[RantDescription("The regex pattern for the filter.")]
			string regexPattern,
			[RantDescription("The regex option string for the filter.")]
			string options)
		{
			Regex regex;
			try
			{
				regex = new Regex(regexPattern, Util.GetRegexOptionsFromString(options));
			}
			catch (Exception ex)
			{
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-invalid-regex", ex.Message);
			}
			sb.QueryBuilder.GetQuery(id).AddFilter(new RegexFilter(regex, false));
		}

		[RantFunction("qsyl")]
		[RantDescription("Adds an syllable count range filter to a constructed query that defines an absolute syllable count.")]
		private static void QuerySyllableFilterAbsolute(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The number of syllables.")]
			int syllables)
		{
			if (syllables < 1) throw new RantRuntimeException(sb, sb.CurrentAction, "err-invalid-syllables-abs", syllables);
			sb.QueryBuilder.GetQuery(id).AddFilter(new RangeFilter(syllables, syllables));
		}

		[RantFunction("qsyl")]
		[RantDescription("Adds a syllable count range filter to a constructed query.")]
		private static void QuerySyllableFilterRange(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The minimum syllable count.")]
			int minSyllables, 
			[RantDescription("The maximum syllable count.")]
			int maxSyllables)
		{
			if (minSyllables < 1 || maxSyllables < 1 || minSyllables > maxSyllables)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-invalid-syllables-range", minSyllables, maxSyllables);
			sb.QueryBuilder.GetQuery(id).AddFilter(new RangeFilter(minSyllables, maxSyllables));
		}

		[RantFunction("qsylmin")]
		[RantDescription("Adds a syllable count range filter to a constructed query that defines only a minimum bound.")]
		private static void QuerySyllableFilterMin(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id,
			[RantDescription("The minimum syllable count.")]
			int minSyllables)
		{
			if (minSyllables < 1)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-invalid-syllables-min", minSyllables);
			sb.QueryBuilder.GetQuery(id).AddFilter(new RangeFilter(minSyllables, null));
		}

		[RantFunction("qsylmax")]
		[RantDescription("Adds a syllable count range filter to a constructed query that defines only a maximum bound.")]
		private static void QuerySyllableFilterMax(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id,
			[RantDescription("The maximum syllable count.")]
			int maxSyllables)
		{
			if (maxSyllables < 1)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-invalid-syllables-max", maxSyllables);
			sb.QueryBuilder.GetQuery(id).AddFilter(new RangeFilter(null, maxSyllables));
		}

		[RantFunction("qphr")]
		[RantDescription("Adds a phrasal complement to a constructed query.")]
		private static void QueryPhrasalComplement(Sandbox sb,
			[RantDescription("The ID string for the constructed query.")]
			string id, 
			[RantDescription("The phrasal complement pattern.")]
			RST complement)
		{
			sb.QueryBuilder.GetQuery(id).Complement = complement;
		}

		#endregion

		#region Variables

		[RantFunction("vexists")]
		[RantDescription("Prints a boolean value indicating whether a variable with the specified name exists.")]
		private static void VarExists(Sandbox sb, 
			[RantDescription("The name of the variable to check.")]
			string name)
		{
			sb.Print(sb.Objects[name] == null ? FALSE : TRUE);
		}

		[RantFunction("vl")]
		[RantDescription("Creates a new list.")]
		private static void VariableList(Sandbox sb, 
			[RantDescription("The name of the list.")]
			string name)
		{
			sb.Objects[name] = new RantObject(new List<RantObject>());
		}

		[RantFunction("vl")]
		[RantDescription("Creates a new list with a specified length.")]
		private static void VariableList(Sandbox sb, 
			[RantDescription("The name of the list.")]
			string name, 
			[RantDescription("The length of the list.")]
			int length)
		{
			if (length < 0) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-invalid-length", length);
			sb.Objects[name] = new RantObject(Enumerable.Repeat<RantObject>(null, length).ToList());
		}

		[RantFunction("ladd", "ladds")]
		[RantDescription("Adds one or more strings to a list.")]
		private static void ListAdd(Sandbox sb, 
			[RantDescription("The list to add to.")]
			RantObject listObj, 
			[RantDescription("The strings to add.")]
			params string[] values)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.AddRange(values.Select(s => new RantObject(s)).ToList());
		}

		[RantFunction("laddn")]
		[RantDescription("Adds one or more numbers to a list.")]
		private static void ListAddNumber(Sandbox sb,
			[RantDescription("The list to add to.")]
			RantObject listObj, 
			[RantDescription("The numbers to add.")]
			params double[] values)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.AddRange(values.Select(s => new RantObject(s)).ToList());
		}

		[RantFunction("laddp")]
		[RantDescription("Adds one or more patterns to a list.")]
		private static void ListAddPattern(Sandbox sb,
			[RantDescription("The list to add to.")]
			RantObject listObj, 
			[RantDescription("The patterns to add.")]
			params RST[] values)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.AddRange(values.Select(s => new RantObject(s)).ToList());
		}

		[RantFunction("laddv")]
		[RantDescription("Adds one or more variables to a list.")]
		private static void ListAddPattern(Sandbox sb,
			[RantDescription("The list to add to.")]
			RantObject listObj,
			[RantDescription("The variables to add.")]
			params RantObject[] values)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.AddRange(values.Select(s => new RantObject(s)).ToList());
		}

		[RantFunction("lpre", "lpres")]
		[RantDescription("Prepends a string to a list.")]
		private static void ListPrepend(Sandbox sb, RantObject listObj, string value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.Insert(0, new RantObject(value));
		}

		[RantFunction("lpren")]
		[RantDescription("Prepends a number to a list.")]
		private static void ListPrependNumber(Sandbox sb, RantObject listObj, double value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.Insert(0, new RantObject(value));
		}

		[RantFunction("lprep")]
		[RantDescription("Prepends a pattern to a list.")]
		private static void ListPrependPattern(Sandbox sb, RantObject listObj, RST value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.Insert(0, new RantObject(value));
		}

		[RantFunction("lget")]
		[RantDescription("Prints a list item from the specified index.")]
		private static void ListGet(Sandbox sb, RantObject listObj, int index)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			sb.Print(list[index].PrintableValue);
		}

		[RantFunction("lset")]
		[RantDescription("Sets the item at a specified index in a list.")]
		private static void ListSet(Sandbox sb, RantObject listObj, int index, string value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list[index] = new RantObject(value);
		}

		[RantFunction("lsetn")]
		[RantDescription("Sets the item at a specified index in a list to a number.")]
		private static void ListSetNumber(Sandbox sb, RantObject listObj, int index, double value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list[index] = new RantObject(value);
		}

		[RantFunction("lsetp")]
		[RantDescription("Sets the item at a specified index in a list to a pattern.")]
		private static void ListSetPattern(Sandbox sb, RantObject listObj, int index, RST value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list[index] = new RantObject(value);
		}

		[RantFunction("lsetv")]
		[RantDescription("Sets the item at a specified index in a list to a variable.")]
		private static void ListSetVar(Sandbox sb, RantObject listObj, int index, RantObject value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list[index] = value;
		}

		[RantFunction("lins")]
		[RantDescription("Inserts a string at the specified index in a list.")]
		private static void ListInsert(Sandbox sb, RantObject listObj, int index, string value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index > list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list.Insert(index, new RantObject(value));
		}
	
		[RantFunction("linsn")]
		[RantDescription("Inserts a number at the specified index in a list.")]
		private static void ListInsert(Sandbox sb, RantObject listObj, int index, double value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index > list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list.Insert(index, new RantObject(value));
		}

		[RantFunction("linsp")]
		[RantDescription("Inserts a pattern at the specified index in a list.")]
		private static void ListInsertPattern(Sandbox sb, RantObject listObj, int index, RST value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index > list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list.Insert(index, new RantObject(value));
		}

		[RantFunction("linsv")]
		[RantDescription("Inserts a variable at the specified index in a list.")]
		private static void ListInsertVar(Sandbox sb, RantObject listObj, int index, RantObject value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index > list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			list.Insert(index, value);
		}

		[RantFunction("lfind")]
		[RantDescription("Searches a list for the specified value and prints the index if found. Otherwise, prints -1.")]
		private static void ListFind(Sandbox sb, RantObject listObj, string value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			for (int i = 0; i < list.Count; i++)
			{
				if (String.Equals(list[i].PrintableValue.ToString(), value, StringComparison.Ordinal))
				{
					sb.Print(i);
					return;
				}
			}
			sb.Print(-1);
		}

		[RantFunction("lfindv")]
		[RantDescription("Searches a list for the specified variable and prints the index if found. Otherwise, prints -1.")]
		private static void ListFind(Sandbox sb, RantObject listObj, RantObject value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] == value)
				{
					sb.Print(i);
					return;
				}
			}
			sb.Print(-1);
		}

		[RantFunction("lfindi")]
		[RantDescription("Searches a list for the specified value, ignoring case, and prints the index if found. Otherwise, prints -1.")]
		private static void ListFindIgnoreCase(Sandbox sb, RantObject listObj, string value)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			for (int i = 0; i < list.Count; i++)
			{
				if (String.Equals(list[i].PrintableValue.ToString(), value, StringComparison.OrdinalIgnoreCase))
				{
					sb.Print(i);
					return;
				}
			}
			sb.Print(-1);
		}

		[RantFunction("lclr")]
		[RantDescription("Clears the specified list.")]
		private static void ListClear(Sandbox sb, RantObject listObj)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			list.Clear();
		}

		[RantFunction("lpop")]
		[RantDescription("Removes the last item from a list.")]
		private static void ListPop(Sandbox sb, RantObject listObj)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (list.Count == 0) return;
			list.RemoveAt(list.Count - 1);
		}

		[RantFunction("lpopf")]
		[RantDescription("Removes the first item from a list.")]
		private static void ListPopStart(Sandbox sb, RantObject listObj)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (list.Count == 0) return;
			list.RemoveAt(0);
		}

		[RantFunction("lcpy")]
		[RantDescription("Copies an item from a list into a variable.")]
		private static void ListCopyItemToVar(Sandbox sb, RantObject listObj, int index, string variable)
		{
			var list = listObj.Value as List<RantObject> ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			if (index < 0 || index >= list.Count) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-index-out-of-range", list.Count - 1, index);
			sb.Objects[variable] = list[index];
		}

		[RantFunction("lclone")]
		[RantDescription("Clones a list to another variable.")]
		private static void ListCopy(Sandbox sb, RantObject listObj, string variable)
		{
			if (listObj.Type != RantObjectType.List)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			sb.Objects[variable] = listObj.Clone();
		}

		[RantFunction("lfilter")]
		[RantDescription("Filters out elements of a list when the condition returns false. Creates new list with results.")]
		private static IEnumerator<RST> ListFilter(Sandbox sb,
			[RantDescription("The name of the list object to filter.")]
			string listName,
			[RantDescription("The name of the list that will contain the filtered result.")]
			string outputListName,
			[RantDescription("The name of the variable that will contain the current item within the condition.")]
			string varname,
			[RantDescription("The condition that will be checked for each item.")]
			RST condition)
		{
			var listObj = sb.Objects[listName];
			if (listObj == null)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-missing-var", listName);
			if (listObj.Type != RantObjectType.List)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			var list = listObj.Value as List<RantObject>;
			var newList = new List<RantObject>(list.Count);
			foreach(var val in list)
			{
				sb.Objects.EnterScope();
				sb.Objects[varname] = val;
				sb.AddOutputWriter();
				yield return condition;
				var output = sb.Return();
				sb.Objects.ExitScope();

				if(output == TRUE)
					newList.Add(val);
				else if (output != FALSE) // if output is neither, it's not a boolean
					throw new RantRuntimeException(sb, condition, "err-runtime-unexpected-type", RantObjectType.Boolean, RantObjectType.String);
			}

			sb.Objects[outputListName] = new RantObject(newList);
		}

		[RantFunction("lfilter")]
		[RantDescription("Filters out elements of a list when the condition returns false. Mutates list.")]
		private static IEnumerator<RST> ListFilter(Sandbox sb,
			[RantDescription("The name of the list object to filter.")]
			string listName,
			[RantDescription("The name of the variable that will contain the current item within the condition.")]
			string varname,
			[RantDescription("The condition that will be checked for each item.")]
			RST condition)
		{
			return ListFilter(sb, listName, listName, varname, condition);
		}

		[RantFunction("lmap")]
		[RantDescription("Runs each item in the input list through the body and adds results to output list.")]
		private static IEnumerator<RST> ListMap(Sandbox sb,
			[RantDescription("The name of the list object to map.")]
			string listName,
			[RantDescription("The name of the list that will contain the mapped result.")]
			string outputListName,
			[RantDescription("The name of the variable that will contain the current item within the body.")]
			string varname,
			[RantDescription("The body that will be run for each item.")]
			RST body)
		{
			var listObj = sb.Objects[listName];
			if (listObj == null)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-missing-var", listName);
			if (listObj.Type != RantObjectType.List)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			var list = listObj.Value as List<RantObject>;
			var newList = new List<RantObject>(list.Count);
			foreach (var val in list)
			{
				sb.Objects.EnterScope();
				sb.Objects[varname] = val;
				sb.AddOutputWriter();
				yield return body;
				var output = sb.Return();
				sb.Objects.ExitScope();

				newList.Add(new RantObject(output.Main));
			}

			sb.Objects[outputListName] = new RantObject(newList);
		}

		[RantFunction("lmap")]
		[RantDescription("Replaces each item in the input list with its value when run through body.")]
		private static IEnumerator<RST> ListMap(Sandbox sb,
			[RantDescription("The name of the list object to map.")]
			string listName,
			[RantDescription("The name of the variable that will contain the current item within the body.")]
			string varname,
			[RantDescription("The body that will be run for each item.")]
			RST body)
		{
			return ListMap(sb, listName, listName, varname, body);
		}

		[RantFunction("lrand")]
		[RantDescription("Returns a random value from the specified list.")]
		private static IEnumerator<RST> ListRandom(Sandbox sb,
			[RantDescription("The list to pick from.")]
			RantObject obj)
		{
			if (obj.Type != RantObjectType.List)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, obj.Type);
			var list = obj.Value as List<RantObject>;
			var item = list[sb.RNG.Next(list.Count)];
			if (item.Type == RantObjectType.Action)
				yield return item.Value as RST;
			else
				sb.Print(item.PrintableValue);
		}

		[RantFunction("split")]
		[RantDescription("Splits the specified string by the given delimiter.")]
		private static void StringSplit(Sandbox sb,
			[RantDescription("The name of the variable that will contain the output list.")]
			string listName,
			[RantDescription("The delimiter.")]
			string delimiter,
			[RantDescription("The string to split.")]
			string input)
		{
			var results = input.Split(new[] { delimiter }, StringSplitOptions.None);
			sb.Objects[listName] = new RantObject(results.Select(r => new RantObject(r)).ToList());
		}

		[RantFunction("split")]
		[RantDescription("Splits the specified string into a list of chars.")]
		private static void StringSplit(Sandbox sb,
			[RantDescription("The name of the variable that will contain the output list.")]
			string listName,
			[RantDescription("The string to split.")]
			string input)
		{
			var chars = input.ToCharArray();
			sb.Objects[listName] = new RantObject(chars.Select(c => new RantObject(c.ToString())).ToList());
		}

		[RantFunction("join")]
		[RantDescription("Joins the specified list into a string seperated by the delimiter and returns it.")]
		private static void StringJoin(Sandbox sb,
			[RantDescription("The list to join.")]
			RantObject listObj,
			[RantDescription("The delimiter.")]
			string delimiter)
		{
			if (listObj.Type != RantObjectType.List)
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-unexpected-type", RantObjectType.List, listObj.Type);
			var list = listObj.Value as List<RantObject>;
			var result = string.Join(delimiter, list.Select(a => a.PrintableValue));
			sb.Print(result);
		}

		[RantFunction("join")]
		[RantDescription("Joins the specified list into a string.")]
		private static void StringJoin(Sandbox sb,
			[RantDescription("The list to join.")]
			RantObject listObj)
		{
			StringJoin(sb, listObj, "");
		}

		[RantFunction("vlen")]
		[RantDescription("Gets the length of the specified variable.")]
		private static void VariableLength(Sandbox sb, RantObject obj)
		{
			sb.Print(obj.Length);
		}

		[RantFunction("typeof")]
		[RantDescription("Gets the type of the specified variable.")]
		private static void GetVariableType(Sandbox sb, string name)
		{
			sb.Print(Util.CamelToSnake(sb.Objects[name]?.Type.ToString() ?? "???"));
		}

		[RantFunction("rvr")]
		[RantDescription("Rotates the values of a list of variables once to the right.")]
		private static void RotateVariablesRight(Sandbox sb,
			[RantDescription("The list of the names of variables whose values will be rotated in order.")] params string[] varNames)
		{
			if (varNames.Length < 2) return;
			var rightmostObject = sb.Objects[varNames[varNames.Length - 1]];
			for (int i = varNames.Length - 1; i >= 1; i--)
			{
				sb.Objects[varNames[i]] = sb.Objects[varNames[i - 1]];
			}
			sb.Objects[varNames[0]] = rightmostObject;
		}

		[RantFunction("rvl")]
		[RantDescription("Rotates the values of a list of variables once to the left.")]
		private static void RotateVariablesLeft(Sandbox sb,
			[RantDescription("The list of the names of variables whose values will be rotated in order.")] params string[] varNames)
		{
			if (varNames.Length < 2) return;
			var leftmostObject = sb.Objects[varNames[0]];
			for (int i = 1; i < varNames.Length - 1; i++)
			{
				sb.Objects[varNames[i - 1]] = sb.Objects[varNames[i]];
			}
			sb.Objects[varNames[varNames.Length - 1]] = leftmostObject;
		}

		[RantFunction("vs")]
		[RantDescription("Creates a new string variable with the specified name and value.")]
		private static void VariableSet(Sandbox sb,
			[RantDescription("The name of the variable.")] string name,
			[RantDescription("The value of the variable.")] string value)
		{
			sb.Objects[name] = new RantObject(value);
		}

		[RantFunction("vb")]
		[RantDescription("Creates a new string variable with the specified name and value.")]
		private static void VariableSet(Sandbox sb,
			[RantDescription("The name of the variable.")] string name,
			[RantDescription("The value of the variable.")] bool value)
		{
			sb.Objects[name] = value ? RantObject.True : RantObject.False;
		}

		[RantFunction("vn")]
		[RantDescription("Creates a new number variable with the specified name and value.")]
		private static void VariableSet(Sandbox sb,
			[RantDescription("The name of the variable.")] string name,
			[RantDescription("The value of the variable.")] double value)
		{
			sb.Objects[name] = new RantObject(value);
		}

		[RantFunction("vn")]
		[RantDescription("Creates a new number variable with a random value between the specified minimum and maximum bounds.")]
		private static void VariableSet(Sandbox sb,
			[RantDescription("The name of the variable.")] string name,
			[RantDescription("The minimum bound of the value.")] int min,
			[RantDescription("The maximum bound of the value.")] int max)
		{
			sb.Objects[name] = new RantObject(sb.RNG.Next(min, max + 1));
		}

		[RantFunction("vp")]
		[RantDescription("Creates a new pattern variable with the specified callback.")]
		private static void VariableSetLazy(Sandbox sb,
			[RantDescription("The name of the variable.")] string name,
			[RantDescription("The value of the variable.")] RST value)
		{
			sb.Objects[name] = new RantObject(value);
		}

		[RantFunction("vcpy")]
		[RantDescription("Copies the value of the variable with the first name to the variable with the second name.")]
		private static void VariableCopy(Sandbox sb,
			[RantDescription("The variable to copy from.")] string a,
			[RantDescription("The variable to copy to.")] string b)
		{
			sb.Objects[b] = sb.Objects[a].Clone();
		}

		[RantFunction("v")]
		[RantDescription("Prints the value of the specified variable.")]
		private static IEnumerator<RST> VariableGet(Sandbox sb,
			[RantDescription("The name of the variable to retrieve.")] string name)
		{
			var o = sb.Objects[name] ?? throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-missing-var", name);

			if (o.Type == RantObjectType.Action)
			{
				yield return o.Value as RST;
			}
			else
			{
				sb.Print(o.PrintableValue);
			}
		}

		[RantFunction("add")]
		[RantDescription("Prints the num of the specified values.")]
		private static void AddVal(Sandbox sb,
			[RantDescription("The first operand.")] double a,
			[RantDescription("The second operand.")] double b)
		{
			sb.Print(a + b);
		}

		[RantFunction("vadd")]
		[RantDescription("Adds a number to the specified variable.")]
		private static void AddVar(Sandbox sb,
			[RantDescription("The name of the variable to add to.")] string a,
			[RantDescription("The value to add.")] double b)
		{
			sb.Objects[a] += new RantObject(b);
		}

		[RantFunction("sub")]
		[RantDescription("Prints the difference of the specified values.")]
		private static void SubVal(Sandbox sb,
			[RantDescription("The first operand.")] double a,
			[RantDescription("The second operand.")] double b)
		{
			sb.Print(a - b);
		}

		[RantFunction("vsub")]
		[RantDescription("Subtracts a number from the specified variable.")]
		private static void SubVar(Sandbox sb,
			[RantDescription("The name of the variable to subtract from.")] string a,
			[RantDescription("The value to subtract.")] double b)
		{
			sb.Objects[a] -= new RantObject(b);
		}

		[RantFunction("mul")]
		[RantDescription("Prints the product of the specified numbers.")]
		private static void MulVal(Sandbox sb,
			[RantDescription("The first operand.")] double a,
			[RantDescription("The second operand.")] double b)
		{
			sb.Print(a * b);
		}

		[RantFunction("vmul")]
		[RantDescription("Multiplies the specified variable by a number.")]
		private static void MulVar(Sandbox sb,
			[RantDescription("The name of the variable to multiply.")] string a,
			[RantDescription("The value to multiply by.")] double b)
		{
			sb.Objects[a] *= new RantObject(b);
		}

		[RantFunction("div")]
		[RantDescription("Prints the quotient of the two specified numbers.")]
		private static void DivVal(Sandbox sb,
			[RantDescription("The dividend.")] double a,
			[RantDescription("The divisor.")] double b)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (b == 0.0) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-div-by-zero");
			sb.Print(a / b);
		}

		[RantFunction("vdiv")]
		[RantDescription("Divides the specified variable by a number.")]
		private static void DivVar(Sandbox sb,
			[RantDescription("The name of the variable to divide.")] string a,
			[RantDescription("The divisor.")] double b)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (b == 0.0) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-div-by-zero");
			sb.Objects[a] /= new RantObject(b);
		}

		[RantFunction("mod")]
		private static void ModVal(Sandbox sb, double a, double b)
		{
			sb.Print(a % b);
		}

		[RantFunction("vmod")]
		private static void ModVar(Sandbox sb, string a, double b)
		{
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (b == 0.0) throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-div-by-zero");
			sb.Objects[a] %= new RantObject(b);
		}

		[RantFunction("swap")]
		[RantDescription("Swaps the values of the variables with the two specified names.")]
		private static void Swap(Sandbox sb,
			[RantDescription("The name of the first variable.")] string a,
			[RantDescription("The name of the second variable.")] string b)
		{
			var temp = sb.Objects[a];
			sb.Objects[a] = sb.Objects[b];
			sb.Objects[b] = temp;
		}

		[RantFunction("veq")]
		[RantDescription("Prints a boolean value indicating whether the variables with the two specified names are equal to each other.")]
		private static void CompEquals(Sandbox sb, RantObject a, RantObject b)
		{
			sb.Print(a.Value.Equals(b.Value) ? TRUE : FALSE);
		}

		[RantFunction("vne")]
		[RantDescription("Prints a boolean value indicating whether the variables with the two specified names are not equal to each other.")]
		private static void CompNotEquals(Sandbox sb, RantObject a, RantObject b)
		{
			sb.Print(a.Value.Equals(b.Value) ? FALSE : TRUE);
		}

		[RantFunction("eq")]
		[RantDescription("Prints a boolean value indicating whether the two values have equal string representations.")]
		private static void CompEquals(Sandbox sb, string a, string b)
		{
			sb.Print(String.Equals(a, b, StringComparison.Ordinal) ? TRUE : FALSE);
		}

		[RantFunction("eqi")]
		[RantDescription("Prints a boolean value indicating whether the two values have equal string representations, ignoring case.")]
		private static void CompEqualsIgnoreCase(Sandbox sb, string a, string b)
		{
			sb.Print(String.Equals(a, b, StringComparison.OrdinalIgnoreCase) ? TRUE : FALSE);
		}

		[RantFunction("ne")]
		[RantDescription("Prints a boolean value indicating whether the two values do not have equal string representations.")]
		private static void CompNotEquals(Sandbox sb, string a, string b)
		{
			sb.Print(String.Equals(a, b, StringComparison.Ordinal) ? FALSE : TRUE);
		}

		[RantFunction("nei")]
		[RantDescription("Prints a boolean value indicating whether two values do not have equal string representations, ignoring case.")]
		private static void CompNotEqualsIgnoreCase(Sandbox sb, string a, string b)
		{
			sb.Print(String.Equals(a, b, StringComparison.OrdinalIgnoreCase) ? FALSE : TRUE);
		}

		[RantFunction("gt")]
		[RantDescription("Prints a boolean value indicating whether the number value of 'a' is greater than the number value of 'b'.")]
		private static void CompGreater(Sandbox sb, double a, double b)
		{
			sb.Print(a > b ? TRUE : FALSE);
		}

		[RantFunction("ge")]
		[RantDescription("Prints a boolean value indicating whether the number value of 'a' is greater than or equal to the number value of 'b'.")]
		private static void CompGreaterEqual(Sandbox sb, double a, double b)
		{
			sb.Print(a >= b ? TRUE : FALSE);
		}

		[RantFunction("lt")]
		[RantDescription("Prints a boolean value indicating whether the number value of 'a' is lesser than the number value of 'b'.")]
		private static void CompLess(Sandbox sb, double a, double b)
		{
			sb.Print(a < b ? TRUE : FALSE);
		}

		[RantFunction("le")]
		[RantDescription("Prints a boolean value indicating whether the number value of 'a' is lesser than or equal to the number value of 'b'.")]
		private static void CompLessEqual(Sandbox sb, double a, double b)
		{
			sb.Print(a <= b ? TRUE : FALSE);
		}

		[RantFunction("not")]
		private static void Not(Sandbox sb, bool a)
		{
			sb.Print(a ? FALSE : TRUE);
		}

		[RantFunction("vnot")]
		private static void Not(Sandbox sb, string a)
		{
			var o = sb.Objects[a];
			if (o == null)
			{
				throw new RantRuntimeException(sb, sb.CurrentAction, "err-runtime-missing-var", a);
			}
			sb.Objects[a] = !o;
		}

		[RantFunction("and")]
		[RantDescription("Prints a boolean value indicating whether both of the boolean values 'a' and 'b' are true.")]
		private static void And(Sandbox sb, 
					[RantDescription("Boolean value to compare.")] bool a,
					[RantDescription("Boolean value to compare.")] params bool[] b)
		{
			bool result = a;
			if (!a)
			{
				sb.Print(FALSE);
				return;
			}
			for (int i = 0; i < b.Length; i++)
			{
				result = result && b[i];
			}
			sb.Print(result ? TRUE : FALSE);
		}

		[RantFunction("nand")]
		private static void Nand(Sandbox sb, bool a, params bool[] b)
		{
			bool result = a;
			if (!a)
			{
				sb.Print(TRUE);
				return;
			}
			for (int i = 0; i < b.Length; i++)
			{
				result = result && b[i];
			}
			sb.Print(result ? FALSE : TRUE);
		}

		[RantFunction("or")]
		[RantDescription("Prints a boolean value indicating whether the boolean 'a' is true or the boolean value 'b' is true.")]
		private static void Or(Sandbox sb, bool a, params bool[] b)
		{
			if (a)
			{
				sb.Print(TRUE);
				return;
			}
			for (int i = 0; i < b.Length; i++)
			{
				if (!b[i]) continue;
				sb.Print(TRUE);
				return;
			}
			sb.Print(FALSE);
		}

		[RantFunction("xor")]
		[RantDescription("Prints a boolean value only when the inputs are different from each other.")]
		private static void Xor(Sandbox sb, bool a, params bool[] b)
		{
			bool x = a;
			for (int i = 0; i < b.Length; i++)
			{
				if (b[i])
				{
					if (x)
					{
						sb.Print(FALSE);
						return;
					}
					x = b[i];
				}
			}
			sb.Print(x ? TRUE : FALSE);
		}

		[RantFunction("maybe")]
		private static void Maybe(Sandbox sb)
		{
			sb.Print(sb.RNG.NextBoolean() ? TRUE : FALSE);
		}

		[RantFunction("if")]
		private static IEnumerator<RST> If(Sandbox sb, bool condition, RST body)
		{
			if (condition) yield return body;
		}

		[RantFunction("ifnot", "ifn")]
		private static IEnumerator<RST> IfNot(Sandbox sb, bool condition, RST body)
		{
			if (!condition) yield return body;
		}

		[RantFunction("if")]
		private static IEnumerator<RST> If(Sandbox sb, bool condition, RST body, RST elseBody)
		{
			yield return condition ? body : elseBody;
		}

		[RantFunction("ifnot", "ifn")]
		private static IEnumerator<RST> IfNot(Sandbox sb, bool condition, RST body, RST elseBody)
		{
			yield return !condition ? body : elseBody;
		}

		[RantFunction("while", "loop")]
		[RantDescription("Runs the body over and over while condition remains true.")]
		private static IEnumerator<RST> WhileLoop(Sandbox sb,
			[RantDescription("The condition to check each iteration.")]
			RST condition,
			[RantDescription("The body of the loop.")]
			RST body)
		{
			while(true)
			{
				sb.AddOutputWriter();
				yield return condition;
				var output = sb.Return();

				if (output == FALSE)
					break;
				else if (output == TRUE)
					yield return body;
				else
					throw new RantRuntimeException(sb, condition, "err-runtime-unexpected-type", RantObjectType.Boolean, RantObjectType.String);
			}
		}

		#endregion
	}
}
