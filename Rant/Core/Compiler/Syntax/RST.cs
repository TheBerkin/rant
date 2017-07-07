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
using System.IO;
using System.Linq;
using System.Reflection;

using Rant.Core.IO;

using static Rant.Localization.Txtres;

namespace Rant.Core.Compiler.Syntax
{
    /// <summary>
    /// Represents a Rant Syntax Tree (RST) node for a Rant pattern. This is the base class for all Rant actions.
    /// </summary>
    internal abstract class RST
    {
        private const uint NullRST = 0x4e554c4c;
        private static readonly Dictionary<uint, TypeInfo> _rstTypeMap = new Dictionary<uint, TypeInfo>();
        private static readonly Dictionary<TypeInfo, uint> _rstIDMap = new Dictionary<TypeInfo, uint>();
        internal LineCol Location;

        static RST()
        {
            foreach (var type in typeof(RST).GetTypeInfo().Assembly.GetTypes()
				.Select(t => t.GetTypeInfo())
                .Where(t => !t.IsAbstract && t.IsSubclassOf(typeof(RST))))
            {
                var attr = type.GetCustomAttributes(typeof(RSTAttribute), false).FirstOrDefault() as RSTAttribute;
                if (attr == null) continue;
                _rstTypeMap[attr.TypeCode] = type;
                _rstIDMap[type] = attr.TypeCode;
            }
        }

        public RST(LineCol location)
        {
            Location = location;
        }

        public static void SerializeRST(RST rst, EasyWriter output)
        {
            var stack = new Stack<IEnumerator<RST>>(10);
            stack.Push(rst.SerializeObject(output));
            top:
            while (stack.Count > 0)
            {
                var serializer = stack.Peek();

                while (serializer.MoveNext())
                {
                    if (serializer.Current == null)
                        output.Write(NullRST);
                    else
                    {
                        stack.Push(serializer.Current.SerializeObject(output));
                        goto top;
                    }
                }

                stack.Pop();
            }
        }

        public static RST DeserializeRST(EasyReader input)
        {
            TypeInfo rootType;
            if (!_rstTypeMap.TryGetValue(input.ReadUInt32(), out rootType) || rootType.AsType() != typeof(RstSequence))
                throw new InvalidDataException(GetString("err-pgmload-invalid-rst-root"));

            int rootLine = input.ReadInt32();
            int rootCol = input.ReadInt32();
            int rootIndex = input.ReadInt32();
            var rootRST = Activator.CreateInstance(rootType.AsType(), new LineCol(rootLine, rootCol, rootIndex)) as RstSequence;
            if (rootRST == null) throw new InvalidDataException(GetString("err-pgmload-root-instance-fail"));

            var stack = new Stack<IEnumerator<DeserializeRequest>>(10);
            stack.Push(rootRST.DeserializeObject(input));

            top:
            while (stack.Count > 0)
            {
                var deserializer = stack.Peek();

                while (deserializer.MoveNext())
                {
                    uint code = input.ReadUInt32();

                    if (code == NullRST)
                    {
                        deserializer.Current.SetResult(null);
                        continue;
                    }

                    TypeInfo type;
                    if (!_rstTypeMap.TryGetValue(code, out type))
                        throw new InvalidDataException(GetString("err-pgmload-bad-type", code));

                    int line = input.ReadInt32();
                    int col = input.ReadInt32();
                    int index = input.ReadInt32();
                    var rst = Activator.CreateInstance(type.AsType(), new LineCol(line, col, index)) as RST;
                    if (rst == null) throw new InvalidDataException(GetString("err-pgmload-rst-creation-fail", type.Name));
                    deserializer.Current.SetResult(rst);
                    stack.Push(rst.DeserializeObject(input));
                    goto top;
                }

                stack.Pop();
            }

            return rootRST;
        }

        /// <summary>
        /// Performs the operations defined in the RST, given a specific sandbox to operate upon.
        /// </summary>
        /// <param name="sb">The sandbox on which to operate.</param>
        /// <returns></returns>
        public abstract IEnumerator<RST> Run(Sandbox sb);

        private IEnumerator<RST> SerializeObject(EasyWriter output)
        {
            output.Write(_rstIDMap[GetType().GetTypeInfo()]);
            output.Write(Location.Line);
            output.Write(Location.Column);
            output.Write(Location.Index);
            return Serialize(output);
        }

        private IEnumerator<DeserializeRequest> DeserializeObject(EasyReader input)
        {
            // Type code and location have already been read
            return Deserialize(input);
        }

        protected abstract IEnumerator<RST> Serialize(EasyWriter output);
        protected abstract IEnumerator<DeserializeRequest> Deserialize(EasyReader input);
    }
}