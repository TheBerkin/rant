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
using System.Runtime.CompilerServices;
using System.Text;

using Rant.Core.Utilities;

#pragma warning disable CS0162 // Unreachable code detected

namespace Rant.Vocabulary
{
    public partial class RantDictionaryTable
    {
		/// <summary>
		/// Loads a table from the specified stream.
		/// </summary>
		/// <param name="origin">The origin of the stream. This will typically be a file path or package name.</param>
		/// <param name="stream">The stream to load the table from.</param>
		/// <returns></returns>
        public static RantDictionaryTable FromStream(string origin, Stream stream)
        {
            string name = null; // Stores the table name before final table construction
            int termsPerEntry = 0; // Stores the term count
            var subtypes = new Dictionary<string, int>(); // Stores subtypes before final table construction
            var hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            RantDictionaryTable table = null; // The table object, constructed when first entry is found
            string l; // Current line string
            int line = 0; // Current line number
            int len, i; // Length and character index of current line
            bool dummy = false; // Determines if the next entry is a dummy entry
            string tId = null; // Template ID
            RantDictionaryEntry activeTemplate = null; // Current template
            var templates = new Dictionary<string, RantDictionaryEntry>();
            RantDictionaryEntry currentEntry = null;
            var autoClasses = new HashSet<string>();
            var autoClassStack = new Stack<List<string>>();

            using (var reader = new StreamReader(stream))
            {
                while (!reader.EndOfStream)
                {
                    line++;

                    // Skip blank lines
                    if (Util.IsNullOrWhiteSpace(l = reader.ReadLine())) continue;

                    // Update line info
                    len = l.Length;
                    i = 0;

                    // Skip whitespace at the start of the line
                    while (i < len && char.IsWhiteSpace(l[i])) i++;

                    switch (l[i++])
                    {
                        // Comments
                        case '#':
                            continue;

                        // Directive
                        case '@':
                        {
                            // Read directive name
                            int dPos = i;
                            if (!Tools.ReadDirectiveName(l, len, ref i, out string directiveName))
                                throw new RantTableLoadException(origin, line, dPos + 1, "err-table-missing-directive-name");

                            // Read arguments
                            var args = new List<Argument>();
                            while (Tools.ReadArg(origin, l, len, line, ref i, out Argument arg)) args.Add(arg);

                            switch (directiveName.ToLowerInvariant())
                            {
                                // Table name definition
                                case "name":
                                {
                                    // Do not allow this to appear anywhere except at the top of the file
                                    if (table != null)
                                        throw new RantTableLoadException(origin, line, dPos + 1, "err-table-misplaced-header-directive");
                                    // Do not allow multiple @name directives
                                    if (name != null)
                                        throw new RantTableLoadException(origin, line, dPos + 1, "err-table-multiple-names");
                                    // One argument required
                                    if (args.Count != 1)
                                        throw new RantTableLoadException(origin, line, dPos + 1, "err-table-name-args");
                                    // Must meet standard identifier requirements
                                    if (!Util.ValidateName(args[0].Value))
                                        throw new RantTableLoadException(origin, line, args[0].CharIndex + 1, "err-table-invalid-name", args[0].Value);
                                    name = args[0].Value;
                                    break;
                                }

                                // Subtype definition
                                case "sub":
                                {
                                    // Do not allow this to appear anywhere except at the top of the file
                                    if (table != null)
                                        throw new RantTableLoadException(origin, line, dPos + 1, "err-table-misplaced-header-directive");
                                    // @sub requires at least one argument
                                    if (args.Count == 0)
                                        throw new RantTableLoadException(origin, line, dPos + 1, "err-table-subtype-args");

                                    // If the first argument is a number, use it as the subtype index.
                                    if (Util.ParseInt(args[0].Value, out int termIndex))
                                    {
                                        // Disallow negative term indices
                                        if (termIndex < 0)
                                            throw new RantTableLoadException(origin, line, dPos + 1, "err-table-sub-index-negative", termIndex);
                                        // Requires at least one name
                                        if (args.Count < 2)
                                            throw new RantTableLoadException(origin, line, dPos + 1, "err-table-sub-missing-name");
                                        // If the index is outside the current term index range, increase the number.
                                        if (termIndex >= termsPerEntry)
                                            termsPerEntry = termIndex + 1;
                                        // Assign all following names to the index
                                        for (int j = 1; j < args.Count; j++)
                                        {
                                            // Validate subtype name
                                            if (!Util.ValidateName(args[j].Value))
                                                throw new RantTableLoadException(origin, line, args[j].CharIndex + 1, "err-table-bad-subtype", args[j].Value);
                                            subtypes[args[j].Value] = termIndex;
                                        }
                                    }
                                    else
                                    {
                                        // Add to last index
                                        termIndex = termsPerEntry++;
                                        // Assign all following names to the index
                                        foreach (var a in args)
                                        {
                                            // Validate subtype name
                                            if (!Util.ValidateName(a.Value))
                                                throw new RantTableLoadException(origin, line, a.CharIndex + 1, "err-table-bad-subtype", a.Value);
                                            subtypes[a.Value] = termIndex;
                                        }
                                    }
                                    break;
                                }
                                case "hide":
                                    if (args.Count == 0) break;
                                    foreach (var a in args)
                                    {
                                        if (!Util.ValidateName(a.Value))
                                            throw new RantTableLoadException(origin, line, i, "err-table-invalid-class", a.Value);
                                        hidden.Add(String.Intern(a.Value));
                                    }
                                    break;
                                case "dummy":
                                    if (args.Count != 0)
                                        throw new RantTableLoadException(origin, line, i, "err-table-argc-mismatch", directiveName, 0, args.Count);
                                    dummy = true;
                                    break;
                                case "id":
                                    if (args.Count != 1)
                                        throw new RantTableLoadException(origin, line, i, "err-table-argc-mismatch", directiveName, 1, args.Count);
                                    if (!Util.ValidateName(args[0].Value))
                                        throw new RantTableLoadException(origin, line, args[0].CharIndex + 1, "err-table-bad-template-id", args[0].Value);
                                    tId = args[0].Value;
                                    break;
                                case "using":
                                    if (args.Count != 1)
                                        throw new RantTableLoadException(origin, line, i, "err-table-argc-mismatch", directiveName, 1, args.Count);
                                    if (!Util.ValidateName(args[0].Value))
                                        throw new RantTableLoadException(origin, line, args[0].CharIndex + 1, "err-table-bad-template-id", args[0].Value);
                                    if (!templates.TryGetValue(args[0].Value, out activeTemplate))
                                        throw new RantTableLoadException(origin, line, args[0].CharIndex + 1, "err-table-template-not-found", args[0].Value);
                                    break;
                                case "class":
                                {
                                    var cList = new List<string>();
                                    if (args.Count == 0)
                                        throw new RantTableLoadException(origin, line, i, "err-table-args-expected", directiveName);
                                    foreach (var cArg in args)
                                    {
                                        if (!Tools.ValidateClassName(cArg.Value))
                                            throw new RantTableLoadException(origin, line, cArg.CharIndex + 1, "err-table-invalid-class", cArg.Value);
                                        cList.Add(cArg.Value);
                                        autoClasses.Add(cArg.Value);
                                    }
                                    autoClassStack.Push(cList);
                                    break;
                                }
                                case "endclass":
                                {
                                    if (args.Count == 0)
                                    {
                                        if (autoClassStack.Count > 0)
                                        {
                                            foreach (string cName in autoClassStack.Pop())
                                                autoClasses.Remove(cName);
                                        }
                                    }
                                    break;
                                }
                            }
                            break;
                        }

                        // Entry
                        case '>':
                            Tools.ConstructTable(origin, name, subtypes, ref termsPerEntry, ref table);
                            Tools.ReadTerms(origin, l, len, line, ref i, table, activeTemplate, templates, out currentEntry);
                            if (!dummy) table.AddEntry(currentEntry);
                            foreach (string autoClass in autoClasses) currentEntry.AddClass(autoClass);
                            if (tId != null)
                            {
                                templates[tId] = currentEntry;
                                tId = null;
                            }
                            dummy = false;
                            activeTemplate = null;
                            break;

                        // Property
                        case '-':
                        {
                            Tools.ConstructTable(origin, name, subtypes, ref termsPerEntry, ref table);
                            Tools.SkipSpace(l, len, ref i);

                            // Read property name
                            int dPos = i;
                            if (!Tools.ReadDirectiveName(l, len, ref i, out string propName))
                                throw new RantTableLoadException(origin, line, dPos + 1, "err-table-missing-property-name");

                            // Read arguments
                            var args = new List<Argument>();
                            while (Tools.ReadArg(origin, l, len, line, ref i, out Argument arg)) args.Add(arg);

                            // No args? Skip it.
                            if (args.Count == 0)
                                continue;

                            switch (propName.ToLowerInvariant())
                            {
                                case "class":
                                    foreach (var cArg in args)
                                    {
                                        if (!Tools.ValidateClassName(cArg.Value))
                                            throw new RantTableLoadException(origin, line, cArg.CharIndex + 1, "err-table-invalid-class", cArg.Value);
                                        currentEntry.AddClass(cArg.Value);
                                    }
                                    break;
                                case "weight":
                                {
                                    if (!float.TryParse(args[0].Value, out float weight) || weight <= 0)
                                        throw new RantTableLoadException(origin, line, args[0].CharIndex + 1, "err-table-invalid-weight", args[0].Value);
                                    currentEntry.Weight = weight;
									table.EnableWeighting = true;
                                    break;
                                }
                                case "pron":
                                    if (args.Count != table.TermsPerEntry)
                                        continue;
                                    for (int j = 0; j < currentEntry.TermCount; j++)
                                        currentEntry[j].Pronunciation = args[j].Value;
                                    break;
                                default:
                                    if (args.Count == 1)
                                        currentEntry.SetMetadata(propName, args[0].Value);
                                    else
                                        currentEntry.SetMetadata(propName, args.Select(a => a.Value).ToArray());
                                    break;
                            }
                            break;
                        }
                    }
                }
            }

            // Add hidden classes
            foreach (string hc in hidden)
                table.HideClass(hc);

            table.RebuildCache();

            return table;
        }

        private static class Tools
        {
#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static void ConstructTable(string origin, string name, Dictionary<string, int> subs, ref int termCount, ref RantDictionaryTable table)
            {
                if (table != null) return;
                if (name == null)
                    throw new RantTableLoadException(origin, 1, 1, "err-table-missing-name");

                if (termCount == 0 || subs.Count == 0)
                {
                    subs.Add("default", 0);
                    termCount = 1;
                }

                table = new RantDictionaryTable(name, termCount);

                foreach (var sub in subs)
                    table.AddSubtype(sub.Key, sub.Value);
            }

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static bool ReadDirectiveName(string str, int len, ref int i, out string result)
            {
                result = null;
                if (i >= len) return false;
                int start = i;
                while (i < len && (char.IsLetterOrDigit(str[i]) || str[i] == '_')) i++;
                if (i == start) return false;
                result = str.Substring(start, i - start);
                return true;
            }

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static void SkipSpace(string str, int len, ref int i)
            {
                while (i < len && char.IsWhiteSpace(str[i])) i++;
            }

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static bool ValidateClassName(string input)
            {
                if (input == null || input.Length == 0) return false;
                for (int i = 0; i < input.Length; i++)
				{
					if (i < input.Length - 1)
					{
						if (!char.IsLetterOrDigit(input[i]) && input[i] != '_') return false;
					}
					else if (!char.IsLetterOrDigit(input[i]) && input[i] != '_' && input[i] != '?') return false;
				}
                return true;
            }

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static void ReadTerms(string origin, string str, int len, int line, ref int i,
                RantDictionaryTable table, RantDictionaryEntry activeTemplate, Dictionary<string, RantDictionaryEntry> templates, out RantDictionaryEntry result)
            {
                SkipSpace(str, len, ref i);
                int t = 0;
                var terms = new RantDictionaryTerm[table.TermsPerEntry];
                int split = -1;
                char c = '\0';
                var buffer = new StringBuilder();
                var white = new StringBuilder();
                while (i < len)
                {
                    switch (c = str[i++])
                    {
                        // Inline comment
                        case '#':
                            goto done;
                        // Phrasal split operator
                        case '+':
                            if (split > -1)
                                throw new RantTableLoadException(origin, line, i, "err-table-multiple-splits");
                            white.Length = 0;
                            split = buffer.Length;
                            SkipSpace(str, len, ref i);
                            break;
                        // Term reference
                        case '[':
                        {
                            SkipSpace(str, len, ref i);
                            if (i >= len)
                                throw new RantTableLoadException(origin, line, i, "err-table-incomplete-term-reference");
                            int start = i;
                            if (white.Length > 0)
                            {
                                buffer.Append(white);
                                white.Length = 0;
                            }
                            switch (str[i++])
                            {
                                // Current term from active template
                                case ']':
                                    if (t == -1)
                                        throw new RantTableLoadException(origin, line, start + 1, "err-table-no-template");
                                    buffer.Append(activeTemplate[t].Value);
                                    break;
                                // Custom term from active template
                                case '.':
                                {
                                    if (activeTemplate == null)
                                        throw new RantTableLoadException(origin, line, start + 1, "err-table-no-template");
                                    while (i < len && IsValidSubtypeChar(str[i])) i++; // Read subtype name
                                    if (str[i] != ']')
                                        throw new RantTableLoadException(origin, line, i, "err-table-incomplete-term-reference");
                                    string subName = str.Substring(start + 1, i - start - 1);
                                    if (subName.Length == 0)
                                        throw new RantTableLoadException(origin, line, start + 1, "err-table-empty-subtype-reference");
                                    int templateSubIndex = table.GetSubtypeIndex(subName);
                                    if (templateSubIndex == -1)
                                        throw new RantTableLoadException(origin, line, start + 1, "err-table-nonexistent-subtype", subName);

                                    // Add term value to buffer
                                    buffer.Append(activeTemplate[templateSubIndex].Value);
                                    i++; // Skip past closing bracket
                                    break;
                                }
                                // It is probably a reference to another entry, let's see.
                                default:
                                {
                                    while (i < len && IsValidSubtypeChar(str[i]) || str[i] == '.') i++;
                                    if (str[i] != ']')
                                        throw new RantTableLoadException(origin, line, i, "err-table-incomplete-term-reference");
                                    var id = str.Substring(start, i - start).Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                                    switch (id.Length)
                                    {
                                        // It's just a template ID.
                                        case 1:
                                        {
                                            if (!templates.TryGetValue(id[0], out RantDictionaryEntry entry))
                                                throw new RantTableLoadException(origin, line, start + 1, "err-table-entry-not-found");
                                            // Append term value to buffer
                                            buffer.Append(entry[t].Value);
                                            break;
                                        }
                                        // Template ID and custom subtype
                                        case 2:
                                        {
                                            if (!templates.TryGetValue(id[0], out RantDictionaryEntry entry))
                                                throw new RantTableLoadException(origin, line, start + 1, "err-table-entry-not-found");
                                            int templateSubIndex = table.GetSubtypeIndex(id[1]);
                                            if (templateSubIndex == -1 || templateSubIndex >= table.TermsPerEntry)
                                                throw new RantTableLoadException(origin, line, start + 1, "err-table-nonexistent-subtype", id[1]);
                                            buffer.Append(entry[templateSubIndex].Value);
                                            break;
                                        }
                                        // ???
                                        default:
                                            throw new RantTableLoadException(origin, line, start + 1, "err-table-invalid-term-reference");
                                    }

                                    i++; // Skip past closing bracket
                                    break;
                                }
                            }
                            break;
                        }
                        case '\\':
                        {
                            if (white.Length > 0)
                            {
                                buffer.Append(white);
                                white.Length = 0;
                            }
                            switch (c = str[i++])
                            {
                                case 'n':
                                    buffer.Append('\n');
                                    continue;
                                case 'r':
                                    buffer.Append('\r');
                                    continue;
                                case 't':
                                    buffer.Append('\t');
                                    continue;
                                case 'v':
                                    buffer.Append('\v');
                                    continue;
                                case 'f':
                                    buffer.Append('\f');
                                    continue;
                                case 'b':
                                    buffer.Append('\b');
                                    continue;
                                case 's':
                                    buffer.Append(' ');
                                    continue;
                                case 'u':
                                {
                                    if (i + 4 > len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                    if (!ushort.TryParse(str.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort codePoint))
                                        throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                    buffer.Append((char)codePoint);
                                    i += 4;
                                    continue;
                                }
                                case 'U':
                                {
                                    if (i + 8 > len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                    if (!Util.TryParseSurrogatePair(str.Substring(i, 8), out char high, out char low))
                                        throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                    buffer.Append(high).Append(low);
                                    i += 8;
                                    continue;
                                }
                                default:
                                    buffer.Append(c);
                                    continue;
                            }
                            continue;
                        }
                        case ',':
                            if (t >= terms.Length)
                                throw new RantTableLoadException(origin, line, i, "err-table-too-many-terms", terms.Length, t);
                            terms[t++] = new RantDictionaryTerm(buffer.ToString(), split);
                            buffer.Length = 0;
                            white.Length = 0;
                            split = -1;
                            SkipSpace(str, len, ref i);
                            break;
                        default:
                            if (char.IsWhiteSpace(c))
                                white.Append(c);
                            else
                            {
                                if (white.Length > 0)
                                {
                                    buffer.Append(white);
                                    white.Length = 0;
                                }
                                buffer.Append(c);
                            }
                            continue;
                    }
                }

                done:

                if (t != terms.Length - 1)
                    throw new RantTableLoadException(origin, line, i, "err-table-too-few-terms", terms.Length, t + 1);

                terms[t] = new RantDictionaryTerm(buffer.ToString());

                result = new RantDictionaryEntry(terms);

                // Add classes from template
                if (activeTemplate != null)
                {
                    foreach (string cl in activeTemplate.GetRequiredClasses())
                        result.AddClass(cl, false);
                    foreach (string cl in activeTemplate.GetOptionalClasses())
                        result.AddClass(cl, true);
                }
            }

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
            public static bool IsValidSubtypeChar(char c) => char.IsLetterOrDigit(c) || c == '_';

#if !UNITY
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif

			public static bool ReadArg(string origin, string str, int len, int line, ref int i, out Argument result)
            {
                result = null;
                while (i < len && char.IsWhiteSpace(str[i])) i++;
                if (i == len || str[i] == '#') return false;
                int start = i;
                var buffer = new StringBuilder();
                char c;

                // Handle string literal
                if (str[i] == '\"')
                {
                    if (++i >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-literal");
                    while (i < len)
                    {
                        switch (c = str[i++])
                        {
                            case '\"':
                                while (i < len && char.IsWhiteSpace(str[i])) i++;
                                if (i < len && str[i] == ',') i++;
                                result = new Argument(start, buffer.ToString());
                                return true;
                            case '\\':
                                if (i >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                switch (c = str[i++])
                                {
                                    case 'n':
                                        buffer.Append('\n');
                                        continue;
                                    case 'r':
                                        buffer.Append('\r');
                                        continue;
                                    case 't':
                                        buffer.Append('\t');
                                        continue;
                                    case 'v':
                                        buffer.Append('\v');
                                        continue;
                                    case 'f':
                                        buffer.Append('\f');
                                        continue;
                                    case 'b':
                                        buffer.Append('\b');
                                        continue;
                                    case 's':
                                        buffer.Append(' ');
                                        continue;
                                    case 'u':
                                    {
                                        if (i + 4 >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                        if (!ushort.TryParse(str.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort codePoint))
                                            throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                        buffer.Append((char)codePoint);
                                        i += 4;
                                        continue;
                                    }
                                    case 'U':
                                    {
                                        if (i + 8 >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                        if (!Util.TryParseSurrogatePair(str.Substring(i, 8), out char high, out char low))
                                            throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                        buffer.Append(high).Append(low);
                                        i += 8;
                                        continue;
                                    }
                                    default:
                                        buffer.Append(c);
                                        continue;
                                }
                                break;
                            default:
                                buffer.Append(c);
                                break;
                        }
                    }
                    throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-literal");
                }

                var white = new StringBuilder();

                // If it isn't a string literal, simply read until a comma is reached.
                while (i < len)
                {
                    switch (c = str[i++])
                    {
                        case ',':
                            result = new Argument(start, buffer.ToString());
                            return true;
                        case '#':
                            result = new Argument(start, buffer.ToString());
                            i = len;
                            return true;
                        case '\\':
                        {
                            if (white.Length > 0)
                            {
                                buffer.Append(white);
                                white.Length = 0;
                            }
                            switch (c = str[i++])
                            {
                                case 'n':
                                    buffer.Append('\n');
                                    continue;
                                case 'r':
                                    buffer.Append('\r');
                                    continue;
                                case 't':
                                    buffer.Append('\t');
                                    continue;
                                case 'v':
                                    buffer.Append('\v');
                                    continue;
                                case 'f':
                                    buffer.Append('\f');
                                    continue;
                                case 'b':
                                    buffer.Append('\b');
                                    continue;
                                case 's':
                                    buffer.Append(' ');
                                    continue;
                                case 'u':
                                {
                                    if (i + 4 >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                    if (!ushort.TryParse(str.Substring(i, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ushort codePoint))
                                        throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                    buffer.Append((char)codePoint);
                                    i += 4;
                                    continue;
                                }
                                case 'U':
                                {
                                    if (i + 8 >= len) throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-escape");
                                    if (!Util.TryParseSurrogatePair(str.Substring(i, 8), out char high, out char low))
                                        throw new RantTableLoadException(origin, line, i + 1, "err-table-unrecognized-codepoint");
                                    buffer.Append(high).Append(low);
                                    i += 8;
                                    continue;
                                }
                                default:
                                    buffer.Append(c);
                                    continue;
                            }
							continue;
						}
                        case '\"':
                            throw new RantTableLoadException(origin, line, i + 1, "err-table-incomplete-literal");
                        default:
                            if (char.IsWhiteSpace(c))
                                white.Append(c);
                            else
                            {
                                if (white.Length > 0)
                                {
                                    buffer.Append(white);
                                    white.Length = 0;
                                }
                                buffer.Append(c);
                            }
                            continue;
                    }
                }

                result = new Argument(start, buffer.ToString());
                return true;
            }
		}

		internal sealed class Argument
        {
            public Argument(int charIndex, string value)
            {
                CharIndex = charIndex;
                Value = value;
            }

            public int CharIndex { get; }
            public string Value { get; }
            public override string ToString() => Value;
        }
    }
}