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
using System.Reflection;

using Rant.Core.Utilities;
using Rant.Metadata;

namespace Rant.Core.Framework
{
    internal static partial class RantFunctionRegistry
    {
        private const string TRUE = "true";
        private const string FALSE = "false";

        private static bool _loaded = false;

        private static readonly Dictionary<string, RantFunction> FunctionTable =
            new Dictionary<string, RantFunction>(StringComparer.OrdinalIgnoreCase);

        private static readonly Dictionary<string, string> AliasTable =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        static RantFunctionRegistry()
        {
            Load();
        }

        internal static void Load()
        {
            if (_loaded) return;

            // Get every single private static method in the RantFunctions class
            foreach (var method in typeof(RantFunctionRegistry).GetTypeInfo().GetMethods(BindingFlags.Static | BindingFlags.NonPublic))
            {
                if (!method.IsStatic) continue;
                var attr =
                    method.GetCustomAttributes(typeof(RantFunctionAttribute), false).FirstOrDefault() as RantFunctionAttribute;
                if (attr == null) continue; // Discard methods without a [RantFunction] attribute

                // Compile metadata into RantFunctionInfo
                string name = string.IsNullOrEmpty(attr.Name) ? method.Name.ToLower() : attr.Name;
                var descAttr =
                    method.GetCustomAttributes(typeof(RantDescriptionAttribute), false).FirstOrDefault() as RantDescriptionAttribute;
                var info = new RantFunctionSignature(name, descAttr?.Description ?? string.Empty, method);

                if (Util.ValidateName(name)) RegisterFunction(info);

                foreach (string alias in attr.Aliases.Where(Util.ValidateName))
                    RegisterAlias(alias, info.Name);
            }
            _loaded = true;
        }

        private static void RegisterFunction(RantFunctionSignature func)
        {
            RantFunction group;
            if (!FunctionTable.TryGetValue(func.Name, out group))
                group = FunctionTable[func.Name] = new RantFunction(func.Name);
            group.Add(func);
        }

        private static void RegisterAlias(string alias, string funcName)
        {
            AliasTable[alias] = funcName;
        }

        public static IEnumerable<string> GetAliases(string funcName) =>
            AliasTable.Where(pair => string.Equals(funcName, pair.Value, StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key);

        private static string ResolveAlias(string alias)
        {
            string name;
            return AliasTable.TryGetValue(alias, out name) ? name : alias;
        }

        public static bool FunctionExists(string name) =>
            AliasTable.ContainsKey(name) || FunctionTable.ContainsKey(name);

        public static IEnumerable<IRantFunctionGroup> GetFunctions()
            => FunctionTable.Select(item => item.Value as IRantFunctionGroup);

        public static IEnumerable<string> GetFunctionNames() =>
            FunctionTable.Select(item => item.Key);

        public static IEnumerable<string> GetFunctionNamesAndAliases() =>
            FunctionTable.Select(item => item.Key).Concat(AliasTable.Keys);

        public static string GetFunctionDescription(string funcName, int paramc) =>
            GetFunctionGroup(funcName)?.GetFunction(paramc)?.Description ?? string.Empty;

        public static RantFunction GetFunctionGroup(string name)
        {
            if (name == null) return null;
            RantFunction group;
            return !FunctionTable.TryGetValue(ResolveAlias(name), out group) ? null : group;
        }

        public static RantFunctionSignature GetFunction(string name, int paramc) =>
            GetFunctionGroup(name)?.GetFunction(paramc);
    }
}