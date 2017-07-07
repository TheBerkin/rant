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
using System.Linq;

using Rant.Core;
using Rant.Core.IO;
using Rant.Core.Utilities;
using Rant.Localization;
using Rant.Resources;
using Rant.Vocabulary.Querying;
using Rant.Vocabulary.Utilities;
using System.Collections;

namespace Rant.Vocabulary
{
	/// <summary>
	/// Represents a named collection of dictionary entries.
	/// </summary>
	public sealed partial class RantDictionaryTable : RantResource
	{
		internal const string NSFW = "nsfw";
		internal const string MissingTerm = "[?]";
		private readonly HashSet<RantDictionaryEntry> _entriesHash = new HashSet<RantDictionaryEntry>();
		private readonly List<RantDictionaryEntry> _entriesList = new List<RantDictionaryEntry>(); // TODO: Use for indexing / weighted selection
		private readonly HashSet<string> _hidden = new HashSet<string>(new[] { NSFW });
		private readonly Dictionary<int, HashSet<string>> _subtypeIndexMap = new Dictionary<int, HashSet<string>>();
		private readonly Dictionary<string, int> _subtypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		private readonly ClassCache _cache;
		private bool _dirty = true;
		private SyllableBuckets[] _syllableBuckets;

		/// <summary>
		/// Initializes a new instance of the RantDictionaryTable class with the specified name and term count.
		/// </summary>
		/// <param name="name">The name of the table.</param>
		/// <param name="termsPerEntry">The number of terms to store in each entry.</param>
		/// <param name="hidden">Collection of hidden classes.</param>
		public RantDictionaryTable(string name, int termsPerEntry, HashSet<string> hidden = null)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (termsPerEntry <= 0) throw new ArgumentException(Txtres.GetString("err-bad-term-count"));
			if (!Util.ValidateName(name)) throw new ArgumentException(Txtres.GetString("err-invalid-tablename", name));
			if (hidden != null) _hidden = hidden;
			_cache = new ClassCache();
			TermsPerEntry = termsPerEntry;
			Name = name;

			CreateSyllableBuckets();
		}

		internal RantDictionaryTable()
		{
			// Used by serializer
			_cache = new ClassCache();
		}

		/// <summary>
		/// Gets the name of the table.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the language code associated with the table (not yet used).
		/// </summary>
		public string Language { get; private set; } = "en-US";

		/// <summary>
		/// Gets the hidden classes of the table.
		/// </summary>
		public IEnumerable<string> HiddenClasses => _hidden.AsEnumerable();

		/// <summary>
		/// Gets the number of entries stored in the table.
		/// </summary>
		public int EntryCount => _entriesHash.Count;

		/// <summary>
		/// Gets the number of terms required for entries contained in the current table.
		/// </summary>
		public int TermsPerEntry { get; private set; }

		/// <summary>
		/// Enumerates the entries stored in the table.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<RantDictionaryEntry> GetEntries() => _entriesHash.AsEnumerable();

		/// <summary>
		/// Enumerates the subtypes contained in the current table.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetSubtypes() => _subtypes.Keys;

		/// <summary>
		/// Determines whether the specified class is hidden by the table.
		/// </summary>
		/// <param name="className">The name of the class to check.</param>
		/// <returns></returns>
		public bool IsClassHidden(string className)
		{
			if (className == null) throw new ArgumentNullException(nameof(className));
			return _hidden.Contains(className);
		}

		/// <summary>
		/// Hides the specified class.
		/// </summary>
		/// <param name="className">The name of the class to hide.</param>
		/// <returns></returns>
		public bool HideClass(string className) => Util.ValidateName(className) && _hidden.Add(className);

		/// <summary>
		/// Unhides the specified class.
		/// </summary>
		/// <param name="className">The name of the class to unhide.</param>
		/// <returns></returns>
		public bool UnhideClass(string className) => className != null && _hidden.Remove(className);

		/// <summary>
		/// Determines whether weights are enabled on this table.
		/// </summary>
		public bool EnableWeighting { get; set; } = false;

		/// <summary>
		/// Indicates whether the cache needs to be rebuilt.
		/// </summary>
		public bool CacheNeedsRebuild => _dirty;

		internal HashSet<RantDictionaryEntry> EntriesHash => _entriesHash;

		/// <summary>
		/// Adds the specified entry to the table.
		/// </summary>
		/// <param name="entry">The entry to add to the table.</param>
		/// <returns>True if successfully added; otherwise, False.</returns>
		public bool AddEntry(RantDictionaryEntry entry)
		{
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			if (entry.TermCount != TermsPerEntry) return false;
			if (!_entriesHash.Add(entry)) return false;
			_entriesList.Add(entry);
			_dirty = true;
			return true;
		}

		/// <summary>
		/// Removes the specified entry from the table.
		/// </summary>
		/// <param name="entry">The entry to remove from the table.</param>
		/// <returns>True if successfully removed; otherwise, False.</returns>
		public bool RemoveEntry(RantDictionaryEntry entry)
		{
			if (entry == null) throw new ArgumentNullException(nameof(entry));
			if (!_entriesHash.Remove(entry)) return false;
			_entriesList.Remove(entry);
			_dirty = true;
			return true;
		}

		/// <summary>
		/// Checks if the table contains the specified entry.
		/// </summary>
		/// <param name="entry">The entry to search for.</param>
		/// <returns>True if found, False if not.</returns>
		public bool ContainsEntry(RantDictionaryEntry entry)
		{
			return _entriesHash.Contains(entry ?? throw new ArgumentNullException(nameof(entry)));
		}

		/// <summary>
		/// Searches entries in the current table and enumerates every single distinct class found.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetClasses()
		{
			var lstClasses = new HashSet<string>();

			foreach (string c in _entriesHash.SelectMany(e => e.GetClasses()))
				if (lstClasses.Add(c)) yield return c;
		}

		/// <summary>
		/// Adds a subtype of the specified name to the table.
		/// If a subtype with the name already exists, it will be overwritten.
		/// Subtypes are case insensitive.
		/// If the name is not a valid identifier string, it will not be accepted.
		/// </summary>
		/// <param name="subtypeName">The name of the subtype to add.</param>
		/// <param name="index">The term index to associate with the name.</param>
		/// <returns>
		/// FALSE if the name was not a valid identifier or the index was out of range. TRUE if the operation was
		/// successful.
		/// </returns>
		public bool AddSubtype(string subtypeName, int index)
		{
			if (index < 0 || index >= TermsPerEntry) return false;
			if (subtypeName == null) throw new ArgumentNullException(nameof(subtypeName));
			if (!Util.ValidateName(subtypeName)) return false;
			_subtypes[subtypeName] = index;
			if (!_subtypeIndexMap.TryGetValue(index, out HashSet<string> subs))
				_subtypeIndexMap[index] = subs = new HashSet<string>();
			subs.Add(subtypeName);
			return true;
		}

		/// <summary>
		/// Removes the specified subtype from the table, if it exists.
		/// Subtypes are case insensitive.
		/// </summary>
		/// <param name="subtypeName">The name of the subtype to remove.</param>
		/// <returns>TRUE if the subtype was found and removed. FALSE if the subtype was not found.</returns>
		public bool RemoveSubtype(string subtypeName)
		{
			if (Util.IsNullOrWhiteSpace(subtypeName)) return false;
			if (!_subtypes.ContainsKey(subtypeName)) return false;
			if (_subtypeIndexMap.TryGetValue(_subtypes[subtypeName], out HashSet<string> subs))
				return subs.Remove(subtypeName) && _subtypes.Remove(subtypeName);
			return false;
		}

		/// <summary>
		/// Retrieves the term index assigned to the specified subtype.
		/// If the subtype is not found, the method will return -1.
		/// If the subtype is a null, whitespace, or an empty string, the method will return 0.
		/// </summary>
		/// <param name="subtype">The subtype to look up.</param>
		/// <returns></returns>
		public int GetSubtypeIndex(string subtype)
		{
			if (Util.IsNullOrWhiteSpace(subtype)) return 0;
			if (!Util.ValidateName(subtype)) return -1;
			if (Util.ParseInt(subtype, out int index) && index >= 0) return index;
			return _subtypes.TryGetValue(subtype, out index) ? index : -1;
		}

		/// <summary>
		/// Enumerates the subtypes associated with the specified term index.
		/// </summary>
		/// <param name="index">The index to get subtypes for.</param>
		/// <returns></returns>
		public IEnumerable<string> GetSubtypesForIndex(int index)
		{
			if (index < 0 || index >= TermsPerEntry) yield break;
			if (!_subtypeIndexMap.TryGetValue(index, out HashSet<string> subs)) yield break;
			foreach (string sub in subs) yield return sub;
		}

		/// <summary>
		/// Adds another table's entries to the current table, given that they share the same name and term count.
		/// </summary>
		/// <param name="other">The table whose entries will be added to the current instance.</param>
		/// <returns>True if merge succeeded; otherwise, False.</returns>
		public bool Merge(RantDictionaryTable other)
		{
			if (other.Name != Name || other == this) return false;
			if (other.TermsPerEntry != TermsPerEntry) return false;
			_entriesHash.AddRange(other._entriesHash);
			_entriesList.AddRange(other._entriesHash);
			_dirty = true;
			RebuildCache();
			return true;
		}

		/// <summary>
		/// Optimizes the table. Call this after writing items to the table or removing items from a table.
		/// If you're writing or removing multiple items, call this after all the actions have been performed.
		/// </summary>
		public void RebuildCache()
		{
			if (!_dirty) return;
			_cache.BuildCache(this);
			CreateSyllableBuckets();
			for (int i = 0; i < TermsPerEntry; i++)
				_syllableBuckets[i] = new SyllableBuckets(i, _entriesList);
			_dirty = false;
		}

		internal RantDictionaryTerm Query(RantDictionary dictionary, Sandbox sb, Query query, CarrierState syncState)
		{
			RebuildCache();

			int index = !String.IsNullOrEmpty(query.PluralSubtype) && sb.TakePlural()
				? GetSubtypeIndex(query.PluralSubtype)
				: GetSubtypeIndex(query.Subtype);

			if (index == -1) return null;

			var filtersEnumerable = query.GetNonClassFilters();
			var filters = filtersEnumerable as Filter[] ?? filtersEnumerable.ToArray();

			IEnumerable<RantDictionaryEntry> pool
				= _cache.Filter(query.GetClassFilters().SelectMany(cf => cf.GenerateRequiredSet(sb.RNG)).Distinct(), dictionary, this)?.ToList();

			if (pool == null) return null;

			if (filters.Length > 0)
				pool = pool.Where((e, i) => filters.OrderBy(f => f.Priority).All(f => f.Test(dictionary, this, e, index, query)));

			if (!pool.Any()) return null;

			return query.HasCarrier
				? syncState.GetEntry(query.Carrier, index, pool, sb.RNG, dictionary.EnableWeighting && this.EnableWeighting)?[index]
				: pool.ToList().PickEntry(sb.RNG, dictionary.EnableWeighting && this.EnableWeighting)?[index];
		}

		internal override void SerializeData(EasyWriter writer)
		{
			writer.Write(Name);
			writer.Write(Language);
			writer.Write(TermsPerEntry);
			for(int i = 0; i < TermsPerEntry; i++)
			{
				writer.Write(GetSubtypesForIndex(i).ToArray());
			}
			writer.Write(_hidden.ToArray());

			writer.Write(_entriesList.Count);
			for(int i = 0; i < _entriesList.Count; i++)
			{
				var entry = _entriesList[i];
				for(int j = 0; j < TermsPerEntry; j++)
				{
					var term = entry[j];
					writer.Write(term.Value);
					writer.Write(term.Pronunciation);
					writer.Write(term.ValueSplitIndex);
					writer.Write(term.PronunciationSplitIndex);
				}
				writer.Write(entry.Weight);
				writer.Write(entry.GetRequiredClasses().ToArray());
				writer.Write(entry.GetOptionalClasses().ToArray());
				var metaKeys = entry.GetMetadataKeys().ToArray();
				writer.Write(metaKeys.Length);
				for(int j = 0; j < metaKeys.Length; j++)
				{
					var metaObj = entry.GetMetadata(metaKeys[j]);
					var metaArray = metaObj as IEnumerable;
					writer.Write(metaArray != null);
					writer.Write(metaKeys[j]);
					if (metaArray != null)
					{
						writer.Write(metaArray.OfType<object>().Select(m => m.ToString()).ToArray());
					}
					else
					{
						writer.Write(metaObj.ToString());
					}
				}
			}
		}

		internal override void DeserializeData(EasyReader reader)
		{
			this.Name = reader.ReadString();
			this.Language = reader.ReadString();
			this.TermsPerEntry = reader.ReadInt32();
			for(int i = 0; i < TermsPerEntry; i++)
			{
				foreach(var subtype in reader.ReadStringArray())
				{
					AddSubtype(subtype, i);
				}
			}
			_hidden.AddRange(reader.ReadStringArray());

			int numEntries = reader.ReadInt32();

			for(int i = 0; i < numEntries; i++)
			{
				var terms = new RantDictionaryTerm[TermsPerEntry];
				for(int j = 0; j < TermsPerEntry; j++)
				{
					var value = reader.ReadString();
					var pron = reader.ReadString();
					int valueSplit = reader.ReadInt32();
					int pronSplit = reader.ReadInt32();
					terms[j] = new RantDictionaryTerm(value, pron, valueSplit, pronSplit);
				}
				float weight = reader.ReadSingle();
				var entry = new RantDictionaryEntry(terms)
				{
					Weight = weight
				};

				foreach(var reqClass in reader.ReadStringArray())
				{
					entry.AddClass(reqClass, false);
				}

				foreach (var optClass in reader.ReadStringArray())
				{
					entry.AddClass(optClass, true);
				}

				int metaCount = reader.ReadInt32();

				for(int j = 0; j < metaCount; j++)
				{
					bool isArray = reader.ReadBoolean();
					var key = reader.ReadString();
					if (isArray)
					{
						entry.SetMetadata(key, reader.ReadStringArray());
					}
					else
					{
						entry.SetMetadata(key, reader.ReadString());
					}
				}

				AddEntry(entry);
			}
		}
		

		internal override void Load(RantEngine engine)
		{
			engine.Dictionary.AddTable(this);
		}

		internal void CreateSyllableBuckets()
		{
			if (_syllableBuckets != null) return;
			_syllableBuckets = new SyllableBuckets[TermsPerEntry];
			for (int i = 0; i < TermsPerEntry; i++)
				_syllableBuckets[i] = new SyllableBuckets(i, new RantDictionaryEntry[] { });
		}
	}
}