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
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

using Rant.Core.Formatting;
using System.Reflection;

namespace Rant.Core.Utilities
{
	internal static class Util
	{
		private static readonly Dictionary<Type, HashSet<string>> _enumTable = new Dictionary<Type, HashSet<string>>();

		public static bool IsUppercase(string sample)
		{
			// All-caps?
			if (!sample.Where(char.IsLetter).All(char.IsUpper)) return false;

			int longest = 0;
			int curLength = 0;
			for (int i = 0; i < sample.Length; i++)
			{
				if (char.IsUpper(sample[i]))
				{
					if (++curLength > longest) longest++;
				}
				else
					curLength = 0;
			}

			return longest > 1;
		}

		private static void CacheEnum(Type type)
		{
			if (!type.GetTypeInfo().IsEnum || _enumTable.ContainsKey(type)) return;
			_enumTable[type] = new HashSet<string>(Enum.GetNames(type));
		}

		public static bool TryParseEnum(Type enumType, string modeString, out object value)
		{
			value = null;
			if (!enumType.GetTypeInfo().IsEnum) throw new ArgumentException("TEnum must be an enumerated type.");
			CacheEnum(enumType);
			string name = SnakeToCamel(modeString.Trim());
			var cache = _enumTable[enumType];
			if (!cache.Contains(name)) return false;
			value = Enum.Parse(enumType, name, true);
			return true;
		}

		/// <summary>
		/// Alternative implementation of String.IsNullOrWhitespace for Unity compatability.
		/// </summary>
		/// <param name="value">The string to check.</param>
		/// <returns></returns>
#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static bool IsNullOrWhiteSpace(string value)
		{
			// ReSharper disable once ReplaceWithStringIsNullOrEmpty
			return value == null || value.Length == 0 || value.All(char.IsWhiteSpace);
		}

		public static int HashOf(params object[] objects)
		{
			return unchecked(objects.Select(o => o.GetHashCode()).Aggregate(17, (hash, next) => hash * 31 + next));
		}

		public static bool ParseInt(string str, out int number)
		{
			unchecked
			{
				int n = number = 0;
				int l = str.Length - 1;
				for (int i = l, x = 1; i >= 0; i--, x *= 10)
				{
					switch (str[i])
					{
						case '-':
							if (i == 0)
							{
								number = -n;
								return true;
							}
							return false;

						case '0':
							continue;

						case '1':
							n += x;
							break;

						case '2':
							n += 2 * x;
							break;

						case '3':
							n += 3 * x;
							break;

						case '4':
							n += 4 * x;
							break;

						case '5':
							n += 5 * x;
							break;

						case '6':
							n += 6 * x;
							break;

						case '7':
							n += 7 * x;
							break;

						case '8':
							n += 8 * x;
							break;

						case '9':
							n += 9 * x;
							break;

						default:
							return false;
					}
				}
				number = n;
				return true;
			}
		}

		public static bool ParseDouble(string str, out double d)
		{
			unchecked
			{
				double num = d = 0.0;
				if (str.Length == 0) return false;
				int l = str.Length;
				int ones = l - 1;
				double x = 0.1;

				for (int i = 0; i < l; i++)
				{
					if (str[i] == '.')
					{
						ones = i - 1;
						for (i++; i < l; i++, x /= 10.0)
						{
							switch (str[i])
							{
								case '0':
									continue;

								case '1':
									num += x;
									break;

								case '2':
									num += x * 2;
									break;

								case '3':
									num += x * 3;
									break;

								case '4':
									num += x * 4;
									break;

								case '5':
									num += x * 5;
									break;

								case '6':
									num += x * 6;
									break;

								case '7':
									num += x * 7;
									break;

								case '8':
									num += x * 8;
									break;

								case '9':
									num += x * 9;
									break;

								default:
									return false;
							}
						}
						break;
					}
				}

				x = 1.0;
				for (int i = ones; i >= 0; i--, x *= 10.0)
				{
					switch (str[i])
					{
						case '-':
							if (i == 0)
							{
								d = -num;
								return true;
							}
							return false;

						case '0':
							continue;

						case '1':
							num += x;
							break;

						case '2':
							num += x * 2;
							break;

						case '3':
							num += x * 3;
							break;

						case '4':
							num += x * 4;
							break;

						case '5':
							num += x * 5;
							break;

						case '6':
							num += x * 6;
							break;

						case '7':
							num += x * 7;
							break;

						case '8':
							num += x * 8;
							break;

						case '9':
							num += x * 9;
							break;

						default:
							return false;
					}
				}

				d = num;
				return true;
			}
		}

		public static bool BooleanRep(string input)
		{
			if (IsNullOrWhiteSpace(input)) return false;
			string v = input.Trim();
			if (String.Equals(v, "false", StringComparison.OrdinalIgnoreCase) || v == "0") return false;
			if (String.Equals(v, "true", StringComparison.OrdinalIgnoreCase)) return true;
			return ParseDouble(v, out double d);
		}

		public static string SnakeToCamel(string name)
		{
			if (string.IsNullOrEmpty(name)) return name;
			var sb = new StringBuilder();
			for (int i = 0; i < name.Length; i++)
			{
				if (i == 0)
					sb.Append(char.ToUpper(name[i]));
				else if ((name[i] == '_' || name[i] == '-') && i + 1 < name.Length)
					sb.Append(char.ToUpper(name[++i]));
				else
					sb.Append(char.ToLower(name[i]));
			}
			return sb.ToString();
		}

		public static string CamelToSnake(string camelName)
		{
			string name = camelName.Trim();
			if (IsNullOrWhiteSpace(name)) return name;
			if (name.Length == 1) return name.ToLower();
			var sb = new StringBuilder();
			bool a, b;
			bool last = false;
			for (int i = 0; i < name.Length - 1; i++)
			{
				a = char.IsUpper(name[i]);
				b = char.IsUpper(name[i + 1]);
				if (last && a && !b) sb.Append('-');
				sb.Append(char.ToLower(name[i]));
				if (!a && b) sb.Append('-');
				last = a;
			}
			sb.Append(char.ToLower(name[name.Length - 1]));
			return sb.ToString();
		}

		public static string GetEnumListString(Type enumType)
		{
			if (!enumType.GetTypeInfo().IsEnum) return String.Empty;
			var sb = new StringBuilder();
			foreach (var name in Enum.GetNames(enumType).OrderBy(str => str).Select(CamelToSnake))
			{
				if (sb.Length > 0) sb.Append(", ");
				sb.Append(name);
			}
			return sb.ToString();
		}

#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static bool ValidateName(string input)
		{
			// ReSharper disable ReplaceWithStringIsNullOrEmpty
			return input != null && input.Length > 0 && input.All(c => char.IsLetterOrDigit(c) || c == '_');
			// ReSharper restore ReplaceWithStringIsNullOrEmpty
		}

		public static string Alt(string input, string alternate)
		{
			return string.IsNullOrEmpty(input) ? alternate : input;
		}

		public static int Mod(int a, int b) => (a % b + b) % b;
#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static char ReverseChar(char c)
		{
			switch (c)
			{
				case '(':
					return ')';
				case ')':
					return '(';
				case '[':
					return ']';
				case ']':
					return '[';
				case '{':
					return '}';
				case '}':
					return '{';
				case '<':
					return '>';
				case '>':
					return '<';
				case '«':
					return '»';
				case '»':
					return '«';
				case '‹':
					return '›';
				case '›':
					return '‹';
				case '\u201c':
					return '\u201d';
				case '\u201d':
					return '\u201c';
				case '\u2018':
					return '\u2019';
				case '\u2019':
					return '\u2018';
				default:
					return c;
			}
		}

		public static char GetAccentChar(this Accent accent)
		{
			switch (accent)
			{
				case Accent.Acute:
					return '\u0301';
				case Accent.Circumflex:
					return '\u0302';
				case Accent.Grave:
					return '\u0300';
				case Accent.Ring:
					return '\u030A';
				case Accent.Tilde:
					return '\u0303';
				case Accent.Diaeresis:
					return '\u0308';
				case Accent.Caron:
					return '\u030C';
				case Accent.Macron:
					return '\u0304';
				default:
					return '?';
			}
		}

#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		public static bool TryParseSurrogatePair(string value, out char highSurrogate, out char lowSurrogate)
		{
			highSurrogate = lowSurrogate = '\0';
			if (value?.Length != 8) return false;

			const uint lowSurrogateMask = 0x3ff;
			const uint highSurrogateMask = lowSurrogateMask << 10;
			const uint lowSurrogateOffset = 0xDC00;
			const uint highSurrogateOffset = 0xD800;
			const uint minCodePoint = 0x10000;

			if (!uint.TryParse(value, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint codePoint))
				return false;
			if (codePoint < minCodePoint) return false;

			codePoint -= minCodePoint;
			highSurrogate = (char)(((codePoint & highSurrogateMask) >> 10) + highSurrogateOffset);
			lowSurrogate = (char)((codePoint & lowSurrogateMask) + lowSurrogateOffset);

			return true;
		}

		public static RegexOptions GetRegexOptionsFromString(string optionsString)
		{
			RegexOptions options = RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture;
			if (IsNullOrWhiteSpace(optionsString)) return options;
			for (int i = 0; i < optionsString.Length; i++)
			{
				switch (optionsString[i])
				{
					case 'i':
						options |= RegexOptions.IgnoreCase;
						break;
					case 'm':
						options |= RegexOptions.Multiline;
						break;
				}
			}
			return options;
		}

		// Thank you: https://www.dotnetperls.com/levenshtein
		public static int LevensteinDistance(string s, string t)
		{
			int n = s.Length;
			int m = t.Length;
			var d = new int[n + 1, m + 1];

			if (n == 0) return m;
			if (m == 0) return n;

			for (int i = 0; i <= n; d[i, 0] = i++)
			{
			}

			for (int j = 0; j <= m; d[0, j] = j++)
			{
			}

			for (int i = 1; i <= n; i++)
			{
				for (int j = 1; j <= m; j++)
				{
					int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

					d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
				}
			}
			return d[n, m];
		}
	}
}