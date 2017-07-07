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
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

#pragma warning disable 0168

namespace Rant.Localization
{
    internal static class Txtres
    {
        public const string LanguageResourceNamespace = "Rant.Localization";
        public const string FallbackLanguageCode = "en-US";

        private static readonly Dictionary<string, Dictionary<string, string>> _languages =
            new Dictionary<string, Dictionary<string, string>>();

        private static Dictionary<string, string> _currentTable = new Dictionary<string, string>();
        private static string _langName = CultureInfo.CurrentCulture.Name;

        static Txtres()
        {
            try
            {
                var ass = typeof(Txtres).GetTypeInfo().Assembly;
                string lang = CultureInfo.CurrentCulture.Name;
                using (var stream =
                    ass.GetManifestResourceStream($"{LanguageResourceNamespace}.{CultureInfo.CurrentCulture.Name}.lang")
                    ?? ass.GetManifestResourceStream($"{LanguageResourceNamespace}.{lang = FallbackLanguageCode}.lang"))
                {
                    if (stream == null)
                    {
#if DEBUG
						Console.WriteLine($"Txtres error: Missing language definition Localization/{CultureInfo.CurrentCulture.Name}.lang");
#endif
                        return;
                    }

                    LoadStringTableData(lang, stream, _currentTable);
                }
#if DEBUG
				Console.WriteLine($"Loaded string resources for {CultureInfo.CurrentCulture.Name}");
#endif
            }

            catch (Exception ex)
            {
#if DEBUG
				Console.WriteLine($"Txtres error: {ex.Message}");
#endif
            }
        }

        private static void LoadStringTableData(string lang, Stream stream, Dictionary<string, string> table)
        {
            using (var reader = new StreamReader(stream))
            {
                loop:
                while (!reader.EndOfStream)
                {
                    string line = reader.ReadLine();
                    if (line == null || line.Length == 0) continue;
                    var kv = line.Split(new[] { '=' }, 2);
                    if (kv.Length != 2) continue;
                    string key = kv[0].Trim();
                    if (!key.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')) continue;
                    string valueLiteral = kv[1].Trim();
                    var sb = new StringBuilder();
                    int i = 0;
                    int len = valueLiteral.Length;
                    while (i < len)
                    {
                        if (i == 0 || i == valueLiteral.Length - 1)
                        {
                            if (valueLiteral[i] != '"') goto loop;
                            i++;
                            continue;
                        }
                        switch (valueLiteral[i])
                        {
                            case '\\':
                            {
                                if (i == valueLiteral.Length - 1) goto loop;
                                switch (valueLiteral[i + 1])
                                {
                                    case 'a':
                                        sb.Append('\a');
                                        break;
                                    case 'b':
                                        sb.Append('\b');
                                        break;
                                    case 'f':
                                        sb.Append('\f');
                                        break;
                                    case 'n':
                                        sb.Append('\n');
                                        break;
                                    case 'r':
                                        sb.Append('\r');
                                        break;
                                    case 't':
                                        sb.Append('\t');
                                        break;
                                    case 'v':
                                        sb.Append('\v');
                                        break;
                                    case 'u':
                                    {
                                        if (i + 5 >= valueLiteral.Length) goto loop;
										if (!short.TryParse(valueLiteral.Substring(i + 1, 4),
										NumberStyles.AllowHexSpecifier,
										CultureInfo.InvariantCulture, out short code))
											goto loop;
										sb.Append((char)code);
                                        i += 6;
                                        continue;
                                    }
                                    default:
                                        sb.Append(valueLiteral[i + 1]);
                                        break;
                                }
                                i += 2;
                                continue;
                            }
                            default:
                                sb.Append(valueLiteral[i]);
                                break;
                        }
                        i++;
                    }
                    table[key] = sb.ToString();
                }
                _languages[lang] = table;
            }
        }

        private static void CheckLanguage()
        {
            if (CultureInfo.CurrentCulture.Name == _langName) return;
            try
            {
                _langName = CultureInfo.CurrentCulture.Name;
				if (!_languages.TryGetValue(_langName, out Dictionary<string, string> table))
				{
					using (
						var stream =
							typeof(Txtres).GetTypeInfo().Assembly.GetManifestResourceStream($"{LanguageResourceNamespace}.{_langName}.lang"))
					{
						if (stream == null) return;
						table = new Dictionary<string, string>();
						LoadStringTableData(_langName, stream, table);
					}
				}
				_currentTable = table;
            }
            catch (Exception ex)
            {
#if DEBUG
				Console.WriteLine($"Txtres error: {ex.Message}");
#endif
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public static void ForceLoad()
        {
        }

        public static string GetString(string name)
        {
            if (name == null) return "<null>";
            CheckLanguage();
            string str;
#if !DEBUG
            return _currentTable.TryGetValue(name, out str) ? str : name;
#else
			if (_currentTable.TryGetValue(name, out str)) return str;
			Console.WriteLine($"MISSING STRING: {name}");
			return name;
#endif
        }

        public static string GetString(string name, params object[] args)
        {
            CheckLanguage();
            string str;
            try
            {
                return _currentTable.TryGetValue(name, out str) ? string.Format(str, args) : name;
            }
            catch
            {
                return $"<Format Error ({name})>";
            }
        }
    }
}