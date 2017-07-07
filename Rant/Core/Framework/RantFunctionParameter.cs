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

using Rant.Core.Utilities;
using Rant.Metadata;
using System.Reflection;

namespace Rant.Core.Framework
{
    internal class RantFunctionParameter : IRantParameter
    {
        public RantFunctionParameter(string name, Type nativeType, RantFunctionParameterType rantType, bool isParams = false)
        {
            Name = Util.CamelToSnake(name);
            NativeType = nativeType;
            RantType = rantType;
            IsParams = isParams;
            Description = string.Empty;
        }

        public Type NativeType { get; }
        public RantFunctionParameterType RantType { get; }
        public string Name { get; }
        public bool IsParams { get; }
        public string Description { get; set; }

        public IEnumerable<IRantModeValue> GetEnumValues()
        {
            var typeInfo = NativeType.GetTypeInfo();
            if (!typeInfo.IsEnum) yield break;
            foreach (string value in Enum.GetNames(NativeType))
            {
                yield return new RantModeValue(Util.CamelToSnake(value),
                (typeInfo.GetMember(value)[0].GetCustomAttributes(typeof(RantDescriptionAttribute), true).FirstOrDefault() as
                    RantDescriptionAttribute)?.Description ?? string.Empty);
            }
        }
    }
}