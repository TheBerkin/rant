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
using System.Text;

using Rant.Core.Compiler.Syntax;
using Rant.Core.IO;
using Rant.Core.ObjectModel;
using Rant.Core.Utilities;
using Rant.Metadata;

namespace Rant.Core.Framework
{
    /// <summary>
    /// Contains information for associating a delegate with a Rant function.
    /// </summary>
    internal class RantFunctionSignature : IRantFunction
    {
        private readonly Witchcraft _delegate;
        public bool TreatAsRichardFunction = false;

        public RantFunctionSignature(string name, string description, MethodInfo method)
        {
            // Sanity checks
            if (method == null) throw new ArgumentNullException(nameof(method));
            if (!method.IsStatic) throw new ArgumentException($"({method.Name}) Method must be static.");

            RawMethod = method;

            var parameters = method.GetParameters();
            if (!parameters.Any())
                throw new ArgumentException($"({method.Name}) Cannot use a parameter-less method for a function.");
            if (parameters[0].ParameterType != typeof(Sandbox))
                throw new ArgumentException($"({method.Name}) The first parameter must be of type '{typeof(Sandbox)}'.");

            // Sort out the parameter types for the function
            Parameters = new RantFunctionParameter[parameters.Length - 1];
            RawParameters = parameters;
            for (int i = 1; i < parameters.Length; i++)
            {
                // Resolve Rant parameter type from .NET type
                var type = parameters[i].ParameterType;
                var typeInfo = type.GetTypeInfo();
                if (type.IsArray && i == parameters.Length - 1)
                    type = type.GetElementType();

                RantFunctionParameterType rantType;
                if (type == typeof(RST) || typeInfo.IsSubclassOf(typeof(RST)))
                {
                    rantType = RantFunctionParameterType.Pattern;
                }
                else if (type == typeof(string))
                {
                    rantType = RantFunctionParameterType.String;
                }
                else if (typeInfo.IsEnum)
                {
                    rantType = typeInfo.GetCustomAttributes(typeof(FlagsAttribute), false).Any()
                        ? RantFunctionParameterType.Flags
                        : RantFunctionParameterType.Mode;
                }
                else if (IOUtil.IsNumericType(type))
                {
                    rantType = RantFunctionParameterType.Number;
                }
                else if (type == typeof(bool))
                {
                    rantType = RantFunctionParameterType.Boolean;
                }
                else if (type == typeof(RantObject))
                {
                    rantType = RantFunctionParameterType.RantObject;
                }
                else
                {
                    throw new ArgumentException(
                        $"({method.Name}) Unsupported type '{type}' for parameter '{parameters[i].Name}'. Must be a string, number, enum, or RantAction.");
                }

                // If there is a [RantDescription] attribute on the parameter, retrieve its value. Default to empty string if there isn't one.
                string paramDescription =
                (parameters[i].GetCustomAttributes(typeof(RantDescriptionAttribute), false).FirstOrDefault() as
                    RantDescriptionAttribute)?.Description ?? "";

                // Create Rant parameter
                Parameters[i - 1] = new RantFunctionParameter(parameters[i].Name, type, rantType,
                    HasParamArray =
                        i == parameters.Length - 1 &&
                        parameters[i].GetCustomAttributes(typeof(ParamArrayAttribute), false).FirstOrDefault() != null)
                {
                    Description = paramDescription
                };
            }
            _delegate = Witchcraft.Create(method);
            Name = name;
            Description = description;
        }

        public RantFunctionParameter[] Parameters { get; }
        public ParameterInfo[] RawParameters { get; }
        public MethodInfo RawMethod { get; }
        public string Name { get; }
        public string Description { get; }
        public bool HasParamArray { get; }
        public int ParamCount => Parameters.Length;
        public IEnumerable<IRantParameter> GetParameters() => Parameters;

        public IEnumerator<RST> Invoke(Sandbox sb, object[] arguments)
        {
            return _delegate.Invoke(sb, arguments) as IEnumerator<RST> ?? CreateEmptyIterator();
        }

        private static IEnumerator<RST> CreateEmptyIterator()
        {
            yield break;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append('[').Append(Name);
            for (int i = 0; i < Parameters.Length; i++)
            {
                sb.Append(i == 0 ? ':' : ';').Append(' ');
                sb.Append(Parameters[i].Name);
                if (i == Parameters.Length - 1 && Parameters[i].IsParams)
                    sb.Append("...");
            }
            sb.Append(']');
            return sb.ToString();
        }
    }
}