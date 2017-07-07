using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Rant.Core.Utilities;

namespace Rant.Vocabulary
{
	internal sealed class TableReader : IDisposable
	{
		private readonly List<Argument> _args = new List<Argument>(4);
		private readonly int _line = 1;
		private readonly string _origin;
		private readonly StreamReader _reader;

		public TableReader(string origin, Stream stream)
		{
			if (origin == null) throw new ArgumentNullException(nameof(origin));
			if (stream == null) throw new ArgumentNullException(nameof(stream));
			_origin = origin;
			_reader = new StreamReader(stream);
		}

		public TableDirectiveType CurrentDirectiveType { get; private set; }
		public string CurrentDirectiveName { get; private set; }
		public int CurrentArgCount => _args.Count;

		public void Dispose()
		{
			_reader.Dispose();
		}

		public string GetArgument(int argIndex) => argIndex > -1 && argIndex < _args.Count ? _args[argIndex].Value : null;

		public bool ReadLine()
		{
			while (!_reader.EndOfStream)
			{
				_args.Clear();
				string strLine = _reader.ReadLine();
				if (Util.IsNullOrWhiteSpace(strLine)) continue;
				int i = 0;
				int len = strLine.Length;

				// Skip whitespace at start of line
				SkipSpace(strLine, len, ref i);

				switch (strLine[i++])
				{
					case '#':
						continue;
					case '@':
					{
						CurrentDirectiveType = TableDirectiveType.Directive;
						string id;
						int start = i;
						if (!ReadIndentifier(strLine, len, ref i, out id))
							throw Error(start, "err-table-directive-name");

						break;
					}
					case '>':
						CurrentDirectiveType = TableDirectiveType.Entry;
						SkipSpace(strLine, len, ref i);
						break;
					case '-':
						CurrentDirectiveType = TableDirectiveType.Property;
						SkipSpace(strLine, len, ref i);
						break;
				}

				return true;
			}
			return false;
		}

#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		private static bool ReadIndentifier(string str, int len, ref int i, out string result)
		{
			result = null;
			int start = i;
			if (i >= len || char.IsWhiteSpace(str[i]) || char.IsControl(str[i])) return false;
			while (i < len && !char.IsWhiteSpace(str[i]) && !char.IsControl(str[i])) i++;
			var strResult = str.Substring(start, i - start);
			if (!Util.ValidateName(strResult)) return false;
			result = strResult;
			return true;
		}

#if !UNITY
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
		private static void SkipSpace(string str, int len, ref int i)
		{
			while (i < len && char.IsWhiteSpace(str[i])) i++;
		}

		private RantTableLoadException Error(int lineCharIndex, string messageType, params object[] messageArgs) => new RantTableLoadException(_origin, _line, lineCharIndex + 1, messageType, messageArgs);

		internal sealed class Argument
		{
			public Argument(int charIndex, string value)
			{
				CharIndex = charIndex;
				Value = value;
			}

			public int CharIndex { get; }
			public string Value { get; }
		}
	}

	internal enum TableDirectiveType
	{
		Directive,
		Entry,
		Property
	}
}