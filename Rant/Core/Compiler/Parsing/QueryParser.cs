﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

using Rant.Core.Compiler.Syntax;
using Rant.Core.Stringes;
using Rant.Vocabulary.Querying;
using Rant.Core.Utilities;

namespace Rant.Core.Compiler.Parsing
{
	internal class QueryParser : Parser
	{
		public override IEnumerator<Parser> Parse(RantCompiler compiler, CompileContext context, TokenReader reader, Action<RST> actionCallback)
		{
			var tableName = reader.ReadLoose(R.Text, "query table name");
			var query = new Query();
			query.Name = tableName.Value;
			query.ClassFilter = new ClassFilter();
			query.RegexFilters = new List<_<bool, Regex>>();
			query.Carrier = new Carrier();
			query.Exclusive = reader.TakeLoose(R.Dollar);
			bool subtypeRead = false;
			bool complementRead = false;
			bool endOfQueryReached = false;

			while (!reader.End && !endOfQueryReached)
			{
				var token = reader.ReadLooseToken();

				switch (token.ID)
				{
					// read subtype
					case R.Subtype:
						// if there's already a subtype, throw an error and ignore it
						if (subtypeRead)
						{
							compiler.SyntaxError(token, false, "err-compiler-multiple-subtypes");
							reader.Read(R.Text, "query subtype name");
							break;
						}
						query.Subtype = reader.Read(R.Text, "query subtype").Value;
						subtypeRead = true;
						break;
					// complement
					case R.LeftSquare:
						{
							if (complementRead) compiler.SyntaxError(token, false, "err-compiler-multiple-complements");
							var seq = new List<RST>();
							compiler.AddContext(CompileContext.QueryComplement);
							compiler.SetNextActionCallback(seq.Add);
							yield return Get<SequenceParser>();
							compiler.SetNextActionCallback(actionCallback);
							query.Complement = new RstSequence(seq, Stringe.Between(token, reader.PrevToken));
							complementRead = true;
						}
						break;
					// read class filter
					case R.Hyphen:
						do
						{
							bool blacklist = false;
							// check if it's a blacklist filter
							if (reader.PeekType() == R.Exclamation)
							{
								blacklist = true;
								reader.ReadToken();
							}
							var classFilterName = reader.Read(R.Text, "class filter rule");
							var rule = new ClassFilterRule(classFilterName.Value, !blacklist);
							query.ClassFilter.AddRule(rule);
						}
						while (reader.TakeLoose(R.Pipe)); //fyi: this feature is undocumented
						break;

					// read regex filter
					case R.Without:
					case R.Question:
						{
							bool blacklist = (token.ID == R.Without);

							var regexFilter = reader.Read(R.Regex, "regex filter rule");
							var rule = new _<bool, Regex>(!blacklist, Util.ParseRegex(regexFilter.Value));
							query.RegexFilters.Add(rule);
						}
						break;

					// read syllable range
					case R.LeftParen:
						// There are four possible types of values in a syllable range:
						// (a), (a-), (-b), (a-b)

						// either (a), (a-), or (a-b)
						if (reader.PeekLooseToken().ID == R.Text)
						{
							var firstNumberToken = reader.ReadLooseToken();
							int firstNumber;
							if (!Util.ParseInt(firstNumberToken.Value, out firstNumber))
							{
								compiler.SyntaxError(firstNumberToken, false, "err-compiler-bad-sylrange-value");
							}

							// (a-) or (a-b)
							if (reader.PeekLooseToken().ID == R.Hyphen)
							{
								reader.ReadLooseToken();
								// (a-b)
								if (reader.PeekLooseToken().ID == R.Text)
								{
									var secondNumberToken = reader.ReadLooseToken();
									int secondNumber;
									if (!Util.ParseInt(secondNumberToken.Value, out secondNumber))
									{
										compiler.SyntaxError(secondNumberToken, false, "err-compiler-bad-sylrange-value");
									}

									query.SyllablePredicate = new Range(firstNumber, secondNumber);
								}
								// (a-)
								else
								{
									query.SyllablePredicate = new Range(firstNumber, null);
								}
							}
							// (a)
							else
							{
								query.SyllablePredicate = new Range(firstNumber, firstNumber);
							}
						}
						// (-b)
						else if (reader.PeekLooseToken().ID == R.Hyphen)
						{
							reader.ReadLooseToken();
							var secondNumberToken = reader.ReadLoose(R.Text, "syllable range value");
							int secondNumber;
							if (!Util.ParseInt(secondNumberToken.Value, out secondNumber))
							{
								compiler.SyntaxError(secondNumberToken, false, "err-compiler-bad-sylrange-value");
							}
							query.SyllablePredicate = new Range(null, secondNumber);
						}
						// ()
						else if (reader.PeekLooseToken().ID == R.RightParen)
						{
							compiler.SyntaxError(token, false, "err-compiler-empty-sylrange");
						}
						// (something else)
						else
						{
							compiler.SyntaxError(reader.PeekLooseToken(), false, "err-compiler-unknown-sylrange-token");
						}

						reader.ReadLoose(R.RightParen, "syllable range end");
						break;

					// read carriers
					case R.DoubleColon:
						ReadCarriers(reader, query.Carrier, compiler);
						// this should be the last part of the query, so go to the end
						endOfQueryReached = true;
						break;

					// end of query
					case R.RightAngle:
						endOfQueryReached = true;
						break;

					case R.Whitespace:
						break;

					default:
						compiler.SyntaxError(token, false, "err-compiler-unexpected-token");
						break;
				}
			}

			if (!endOfQueryReached)
			{
				compiler.SyntaxError(reader.PrevToken, true, "err-compiler-eof");
			}

			actionCallback(new RstQuery(query, tableName));
		}

		private void ReadCarriers(TokenReader reader, Carrier carrier, RantCompiler compiler)
		{
			while (!reader.End)
			{
				var token = reader.ReadLooseToken();

				switch (token.ID)
				{
					// match carrier
					case R.Equal:
						{
							var name = reader.Read(R.Text, "carrier name");
							carrier.AddComponent(CarrierComponent.Match, name.Value);
						}
						break;

					// associative and match associative,
					// disassociative and match disassociative
					// divergent and match-divergent
					// relational and match-relational
					case R.At:
						{
							var carrierType = CarrierComponent.Associative;
							// disassociative
							if (reader.PeekToken().ID == R.Exclamation)
							{
								carrierType = CarrierComponent.Dissociative;
								reader.ReadToken();
							}
							// divergent
							else if (reader.PeekToken().ID == R.Plus)
							{
								carrierType = CarrierComponent.Divergent;
								reader.ReadToken();
							}
							else if (reader.PeekToken().ID == R.Question)
							{
								carrierType = CarrierComponent.Relational;
								reader.ReadToken();
							}

							// match
							if (reader.PeekToken().ID == R.Equal)
							{
								if (carrierType == CarrierComponent.Associative)
								{
									carrierType = CarrierComponent.MatchAssociative;
								}
								else if (carrierType == CarrierComponent.Dissociative)
								{
									carrierType = CarrierComponent.MatchDissociative;
								}
								else if (carrierType == CarrierComponent.Divergent)
								{
									carrierType = CarrierComponent.MatchDivergent;
								}
								else if (carrierType == CarrierComponent.Relational)
								{
									carrierType = CarrierComponent.MatchRelational;
								}
								reader.ReadToken();
							}

							var name = reader.Read(R.Text, "carrier name");
							carrier.AddComponent(carrierType, name.Value);
						}
						break;

					// unique and match unique
					case R.Exclamation:
						{
							var carrierType = CarrierComponent.Unique;
							// match unique
							if (reader.PeekToken().ID == R.Equal)
							{
								carrierType = CarrierComponent.MatchUnique;
								reader.ReadToken();
							}

							var name = reader.Read(R.Text, "carrier name");
							carrier.AddComponent(carrierType, name.Value);
						}
						break;

					// rhyming
					case R.Ampersand:
						{
							var name = reader.Read(R.Text, "carrier name");
							carrier.AddComponent(CarrierComponent.Rhyme, name.Value);
						}
						break;

					// we're done, go away
					case R.RightAngle:
						return;

					default:
						compiler.SyntaxError(token, false, "err-compiler-unexpected-token");
						break;
				}
			}
		}
	}
}