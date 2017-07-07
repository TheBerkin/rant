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

namespace Rant.Core.Utilities
{
    /// <summary>
    /// Allows creation of Rant function delegates from reflected methods that can be invoked using a series of boxed
    /// arguments.
    /// </summary>
    internal abstract class Witchcraft
    {
        private static readonly Type[] _funcTypes;
        private static readonly Type[] _voidTypes;

        static Witchcraft()
        {
            var ass = Assembly.GetAssembly(typeof(Witchcraft));
            var lstFuncTypes = new List<Type>();
            var lstVoidTypes = new List<Type>();
            foreach (var type in ass.GetTypes().Where(t => t.IsSubclassOf(typeof(Witchcraft)) && t.IsGenericTypeDefinition))
            {
                if (type.IsSubclassOf(typeof(WitchcraftVoid)))
                    lstVoidTypes.Add(type);
                else
                    lstFuncTypes.Add(type);
            }

            _funcTypes = lstFuncTypes.OrderBy(t => t.GetTypeInfo().GetGenericArguments().Length).ToArray();
            _voidTypes = lstVoidTypes.OrderBy(t => t.GetTypeInfo().GetGenericArguments().Length).ToArray();
        }

        public static Witchcraft Create(MethodInfo methodInfo)
        {
            bool isVoid = methodInfo.ReturnType == typeof(void);
            var types = methodInfo.GetParameters().Select(p => p.ParameterType).ToArray();
            if (types.Length == 0 || types[0] != typeof(Sandbox))
                throw new ArgumentException("Method must have a Sandbox parameter come first.", nameof(methodInfo));

            var argTypes = types.Skip(1).ToArray();
            if (argTypes.Length >= _funcTypes.Length)
                throw new ArgumentException($"Methods with {types.Length} argument(s) are not currently supported.");

            if (argTypes.Length == 0)
            {
                if (isVoid)
                    return new WitchcraftNoParamsVoid(methodInfo);
                return new WitchcraftNoParams(methodInfo);
            }

            var type = isVoid
                ? _voidTypes[argTypes.Length - 1].MakeGenericType(argTypes)
                : _funcTypes[argTypes.Length - 1].MakeGenericType(argTypes);

            return (Witchcraft)Activator.CreateInstance(type, methodInfo);
        }

        public abstract object Invoke(Sandbox sb, object[] args);
    }

    internal class Witchcraft<A> : Witchcraft
    {
        private readonly Func<Sandbox, A, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) => _func(sb, (A)args[0]);
    }

    internal class WitchcraftVoid<A> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0]);
            return null;
        }
    }

    internal class Witchcraft<A, B> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1]);
    }

    internal class WitchcraftVoid<A, B> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2]);
    }

    internal class WitchcraftVoid<A, B, C> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, D, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, D, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, D, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3]);
    }

    internal class WitchcraftVoid<A, B, C, D> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C, D> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C, D>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C, D>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D, E> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, D, E, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, D, E, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, D, E, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4]);
    }

    internal class WitchcraftVoid<A, B, C, D, E> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C, D, E> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C, D, E>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C, D, E>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D, E, F> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, D, E, F, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, D, E, F, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, D, E, F, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C, D, E, F> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C, D, E, F>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C, D, E, F>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D, E, F, G> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, D, E, F, G, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, D, E, F, G, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, D, E, F, G, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F, G> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C, D, E, F, G> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C, D, E, F, G>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C, D, E, F, G>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D, E, F, G, H> : Witchcraft
    {
        private readonly Func<Sandbox, A, B, C, D, E, F, G, H, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (Func<Sandbox, A, B, C, D, E, F, G, H, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, A, B, C, D, E, F, G, H, object>));
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6], (H)args[7]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F, G, H> : WitchcraftVoid
    {
        private readonly Action<Sandbox, A, B, C, D, E, F, G, H> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (Action<Sandbox, A, B, C, D, E, F, G, H>)methodInfo.CreateDelegate(typeof(Action<Sandbox, A, B, C, D, E, F, G, H>));
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6], (H)args[7]);
            return null;
        }
    }
}