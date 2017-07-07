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
using System.Reflection;

using Rant.Core.Utilities;

namespace Rant
{
    /// <summary>
    /// Represents a set of arguments that can be passed to a pattern.
    /// </summary>
    public sealed class RantProgramArgs
    {
        private static readonly Dictionary<Type, Dictionary<string, PropertyInfo>> objPropMap =
            new Dictionary<Type, Dictionary<string, PropertyInfo>>();

        private readonly Dictionary<string, string> _args = new Dictionary<string, string>();

        /// <summary>
        /// Create a new, empty RantPatternArgs instance.
        /// </summary>
        public RantProgramArgs()
        {
        }

        internal RantProgramArgs(object value)
        {
            if (value == null) return;
            var type = value.GetType();
            Dictionary<string, PropertyInfo> map;
            RantArgAttribute attrName;
            if (!objPropMap.TryGetValue(type, out map))
            {
                objPropMap[type] = map = new Dictionary<string, PropertyInfo>();
                foreach (var prop in type.GetTypeInfo().GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead))
                {
#if UNITY
					if ((attrName = prop.GetCustomAttributes(typeof(RantArgAttribute), true).Cast<RantArgAttribute>().FirstOrDefault()) != null)
#else
	                if ((attrName = prop.GetCustomAttributes<RantArgAttribute>().FirstOrDefault()) != null)
#endif

	                {
		                map[attrName.Name] = prop;
	                }
	                else
	                {
		                map[prop.Name] = prop;
	                }
                }
            }

            foreach (var pair in map)
            {
#if UNITY
				var obj = pair.Value.GetValue(value, null);
#else
                var obj = pair.Value.GetValue(value);
#endif

                if (obj != null) _args[pair.Key] = Convert.ToString(obj, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets or sets an argument of the specified name.
        /// </summary>
        /// <param name="key">The name of the argument.</param>
        /// <returns></returns>
        public string this[string key]
        {
            get
            {
                if (Util.IsNullOrWhiteSpace(key)) return string.Empty;
				return _args.TryGetValue(key, out string val) ? val ?? string.Empty : string.Empty;
			}
            set
            {
                if (key == null || string.IsNullOrEmpty(value)) return;
                _args[key] = value;
            }
        }

        /// <summary>
        /// Creates a RantPatternArgs instance from the specified object.
        /// Works with anonymous types!
        /// </summary>
        /// <param name="value">The object to create an argument set from.</param>
        /// <returns></returns>
        public static RantProgramArgs CreateFrom(object value)
        {
            if (value is RantProgramArgs args) return args;
            return new RantProgramArgs(value);
        }

        /// <summary>
        /// Determines whether an argument by the specified name exists in the current list.
        /// </summary>
        /// <param name="key">The name of the argument to search for.</param>
        /// <returns></returns>
        public bool Contains(string key) => _args.ContainsKey(key ?? string.Empty);

        /// <summary>
        /// Removes the specified argument.
        /// </summary>
        /// <param name="key">The name of the argument to remove.</param>
        /// <returns></returns>
        public bool Remove(string key) => _args.Remove(key ?? string.Empty);

        /// <summary>
        /// Clears all values.
        /// </summary>
        public void Clear() => _args.Clear();
    }
}