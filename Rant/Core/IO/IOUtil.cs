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
using System.Reflection;
using System.Runtime.InteropServices;

namespace Rant.Core.IO
{
    internal static class IOUtil
    {
        public static int NumberOfSetBits(uint i)
        {
            i = i - ((i >> 1) & 0x55555555);
            i = (i & 0x33333333) + ((i >> 2) & 0x33333333);
            return (int)(((i + (i >> 4)) & 0x0F0F0F0F) * 0x01010101) >> 24;
        }

        public static bool EndianConvertNeeded(Endian endianness)
        {
            return BitConverter.IsLittleEndian && endianness == Endian.Big ||
                   !BitConverter.IsLittleEndian && endianness == Endian.Little;
        }

        public static bool IsNumericType(Type t)
        {
            switch (Type.GetTypeCode(t))
            {
                case TypeCode.Byte:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Converts the endianness of a series of bytes according to the endianness of the data. This process works both for
        /// system-side and data-side conversions.
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <param name="dataEndianness">The endianness to convert to or from.</param>
        public static void ConvertEndian(byte[] data, Endian dataEndianness)
        {
            if (BitConverter.IsLittleEndian != (dataEndianness == Endian.Little))
                Array.Reverse(data);
        }

        public static void ConvertStructEndians<TStruct>(ref TStruct o)
        {
            var typeInfo = typeof(TStruct).GetTypeInfo();
            if (!typeInfo.IsValueType)
                throw new ArgumentException("TStruct must be a value type.");
            object boxed = o;
            foreach (var field in typeInfo.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            )
            {
                var ftype = field.FieldType;
                if (!IsNumericType(ftype))
                    continue;

                var attrs = field.GetCustomAttributes(true);
                foreach (var attr in attrs)
                {
                    if (attr.GetType() == typeof(EndiannessAttribute))
                    {
                        var endian = ((EndiannessAttribute)attr).Endian;
                        if (EndianConvertNeeded(endian))
                        {
                            // Get the field size, allocate a pointer and a buffer for flipping bytes.
                            int length = Marshal.SizeOf(ftype);
                            var vptr = Marshal.AllocHGlobal(length);
                            var vData = new byte[length];

                            // Fetch the field value and store it.
                            var value = field.GetValue(boxed);

                            // Transfer the field value to the pointer and copy it to the array.
                            Marshal.StructureToPtr(value, vptr, false);
                            Marshal.Copy(vptr, vData, 0, length);

                            // Reverse.
                            Array.Reverse(vData);

                            // Copy it back to the pointer.
                            Marshal.Copy(vData, 0, vptr, length);
                            value = Marshal.PtrToStructure(vptr, ftype);
                            // Plug it back into the field.
                            field.SetValue(boxed, value);
                            // Deallocate the pointer.
                            Marshal.FreeHGlobal(vptr);
                            o = (TStruct)boxed;
                        }
                        break; // Go to the next field.
                    }
                }
            }
        }
    }
}