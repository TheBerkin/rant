using System;
using System.Reflection;

namespace Rant.Core.Utilities
{
	internal class WitchcraftNoParams : Witchcraft
	{
		private readonly Func<Sandbox, object> _func;

		public WitchcraftNoParams(MethodInfo methodInfo)
		{
			_func = (Func<Sandbox, object>)methodInfo.CreateDelegate(typeof(Func<Sandbox, object>));
		}

		public override object Invoke(Sandbox sb, object[] args) => _func(sb);
	}
}