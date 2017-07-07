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
	internal delegate TResult XFunc<in A, out TResult>(A a);

    internal delegate TResult XFunc<in A, in B, out TResult>(A a, B b);

    internal delegate TResult XFunc<in A, in B, in C, out TResult>(A a, B b, C c);

    internal delegate TResult XFunc<in A, in B, in C, in D, out TResult>(A a, B b, C c, D d);

    internal delegate TResult XFunc<in A, in B, in C, in D, in E, out TResult>(A a, B b, C c, D d, E e);

    internal delegate TResult XFunc<in A, in B, in C, in D, in E, in F, out TResult>(A a, B b, C c, D d, E e, F f);

    internal delegate TResult XFunc<in A, in B, in C, in D, in E, in F, in G, out TResult>(
        A a, B b, C c, D d, E e, F f, G g);

    internal delegate TResult XFunc<in A, in B, in C, in D, in E, in F, in G, in H, out TResult>(
        A a, B b, C c, D d, E e, F f, G g, H h);

    internal delegate TResult XFunc<in A, in B, in C, in D, in E, in F, in G, in H, in I, out TResult>(
        A a, B b, C c, D d, E e, F f, G g, H h, I i);

	internal delegate void XAction<in A>(A a);

    internal delegate void XAction<in A, in B>(A a, B b);

    internal delegate void XAction<in A, in B, in C>(A a, B b, C c);

    internal delegate void XAction<in A, in B, in C, in D>(A a, B b, C c, D d);

    internal delegate void XAction<in A, in B, in C, in D, in E>(A a, B b, C c, D d, E e);

    internal delegate void XAction<in A, in B, in C, in D, in E, in F>(A a, B b, C c, D d, E e, F f);

    internal delegate void XAction<in A, in B, in C, in D, in E, in F, in G>(A a, B b, C c, D d, E e, F f, G g);

    internal delegate void XAction<in A, in B, in C, in D, in E, in F, in G, in H>(A a, B b, C c, D d, E e, F f, G g, H h);

    internal delegate void XAction<in A, in B, in C, in D, in E, in F, in G, in H, in I>(
        A a, B b, C c, D d, E e, F f, G g, H h, I i);

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
        private readonly XFunc<Sandbox, A, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) => _func(sb, (A)args[0]);
    }

    internal class WitchcraftVoid<A> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0]);
            return null;
        }
    }

    internal class Witchcraft<A, B> : Witchcraft
    {
        private readonly XFunc<Sandbox, A, B, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1]);
    }

    internal class WitchcraftVoid<A, B> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C> : Witchcraft
    {
        private readonly XFunc<Sandbox, A, B, C, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2]);
    }

    internal class WitchcraftVoid<A, B, C> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D> : Witchcraft
    {
        private readonly XFunc<Sandbox, A, B, C, D, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, D, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, D, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3]);
    }

    internal class WitchcraftVoid<A, B, C, D> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C, D> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C, D>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C, D>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3]);
            return null;
        }
    }

    internal class Witchcraft<A, B, C, D, E> : Witchcraft
    {
        private readonly XFunc<Sandbox, A, B, C, D, E, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, D, E, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, D, E, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4]);
    }

    internal class WitchcraftVoid<A, B, C, D, E> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C, D, E> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C, D, E>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C, D, E>), methodInfo);
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
        private readonly XFunc<Sandbox, A, B, C, D, E, F, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, D, E, F, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, D, E, F, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C, D, E, F> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C, D, E, F>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C, D, E, F>), methodInfo);
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
        private readonly XFunc<Sandbox, A, B, C, D, E, F, G, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, D, E, F, G, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, D, E, F, G, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F, G> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C, D, E, F, G> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C, D, E, F, G>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C, D, E, F, G>), methodInfo);
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
        private readonly XFunc<Sandbox, A, B, C, D, E, F, G, H, object> _func;

        public Witchcraft(MethodInfo methodInfo)
        {
            _func = (XFunc<Sandbox, A, B, C, D, E, F, G, H, object>)Delegate.CreateDelegate(
                typeof(XFunc<Sandbox, A, B, C, D, E, F, G, H, object>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args) =>
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6], (H)args[7]);
    }

    internal class WitchcraftVoid<A, B, C, D, E, F, G, H> : WitchcraftVoid
    {
        private readonly XAction<Sandbox, A, B, C, D, E, F, G, H> _func;

        public WitchcraftVoid(MethodInfo methodInfo)
        {
            _func = (XAction<Sandbox, A, B, C, D, E, F, G, H>)Delegate.CreateDelegate(
                typeof(XAction<Sandbox, A, B, C, D, E, F, G, H>), methodInfo);
        }

        public override object Invoke(Sandbox sb, object[] args)
        {
            _func(sb, (A)args[0], (B)args[1], (C)args[2], (D)args[3],
                (E)args[4], (F)args[5], (G)args[6], (H)args[7]);
            return null;
        }
    }
}