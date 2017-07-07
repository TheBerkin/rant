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

using Rant.Vocabulary.Querying;

namespace Rant.Vocabulary
{
    internal sealed class ClassCache
    {
	    private static readonly Stack<HashSet<RantDictionaryEntry>> _recycle = new Stack<HashSet<RantDictionaryEntry>>(24);
		private readonly Dictionary<string, HashSet<RantDictionaryEntry>> _cache = new Dictionary<string, HashSet<RantDictionaryEntry>>();
        private readonly Dictionary<string, HashSet<RantDictionaryEntry>> _invCache = new Dictionary<string, HashSet<RantDictionaryEntry>>();
		private readonly HashSet<string> _allClasses = new HashSet<string>();
	    
	    private readonly object syncObject = new object();

	    private void ClearAll()
	    {
		    foreach (var pair in _cache)
		    {
			    pair.Value.Clear();
				_recycle.Push(pair.Value);
		    }

		    foreach (var pair in _invCache)
		    {
			    pair.Value.Clear();
				_recycle.Push(pair.Value);
		    }

		    _allClasses.Clear();
	    }

	    private static HashSet<RantDictionaryEntry> GetPool()
	    {
		    return _recycle.Count == 0 ? new HashSet<RantDictionaryEntry>() : _recycle.Pop();
	    }

        public void BuildCache(RantDictionaryTable table)
        {
	        lock (syncObject)
	        {
		        ClearAll();

		        foreach (var entry in table.GetEntries())
		        {
			        foreach (var cl in entry.GetClasses())
			        {
				        HashSet<RantDictionaryEntry> set;
				        _allClasses.Add(cl);
				        if (!_cache.TryGetValue(cl, out set))
				        {
					        set = _cache[cl] = GetPool();
				        }
				        set.Add(entry);
			        }
		        }

		        foreach (var cl in _allClasses)
		        {
			        if (!_invCache.TryGetValue(cl, out HashSet<RantDictionaryEntry> set))
			        {
				        set = _invCache[cl] = GetPool();
					}
			        foreach (var entry in table.GetEntries())
			        {
				        if (!entry.ContainsClass(cl)) set.Add(entry);
			        }
		        }
			}
        }
        
        public IEnumerable<RantDictionaryEntry> Filter(IEnumerable<ClassFilterRule> rules, RantDictionary dictionary, RantDictionaryTable table)
        {
	        lock (syncObject)
	        {
		        int startIndex = 0;
				var ruleArray = rules as ClassFilterRule[] ?? rules.ToArray();
		        HashSet<RantDictionaryEntry> setCached;
		        var set = GetPool();
		        var hide = table.HiddenClasses
			        // Exclude overridden hidden classes
			        .Except(dictionary.IncludedHiddenClasses)
			        // Exclude hidden classes filtered for
			        .Where(cl => !ruleArray.Any(rule => rule.ShouldMatch && String.Equals(rule.Class, cl, StringComparison.OrdinalIgnoreCase))).ToArray();

		        // Get initial pool
				if (hide.Length > 0)
		        {
					// Retrieve the inverse pool of the first hidden class
			        if (!_invCache.TryGetValue(hide[0], out setCached)) setCached = table.EntriesHash;
		        }
		        else if (ruleArray.Length > 0)
				{
					if (ruleArray[0].ShouldMatch)
					{
						if (!_cache.TryGetValue(ruleArray[0].Class, out setCached)) return null;
					}
					else
					{
						if (!_invCache.TryGetValue(ruleArray[0].Class, out setCached)) setCached = table.EntriesHash;
					}
					startIndex = 1;
				}
				else
				{
					setCached = table.EntriesHash;
				}
				
				// Transfer items from cached pool to our new pool and exclude hidden classes
		        foreach (var item in setCached)
		        {
			        if (hide.Length == 0 || !hide.Any(cl => item.ContainsClass(cl))) set.Add(item);
		        }

				// Apply filters by intersecting pools
		        for (int i = startIndex; i < ruleArray.Length; i++)
		        {
			        if (ruleArray[i].ShouldMatch)
			        {
				        if (_cache.TryGetValue(ruleArray[i].Class, out setCached))
				        {
							set.IntersectWith(setCached);
				        }
				        else
				        {
					        return null;
				        }
			        }
			        else
			        {
				        if (_invCache.TryGetValue(ruleArray[i].Class, out setCached))
				        {
					        set.IntersectWith(setCached);
				        }
			        }
		        }

		        return set;
			}
        }
    }
}