using System;
using System.Reflection;

namespace Rant.Core.Utilities
{
	internal class WitchcraftNoParamsVoid : WitchcraftVoid
	{
		private readonly Action<Sandbox> _func;

		public WitchcraftNoParamsVoid(MethodInfo methodInfo)
		{
			_func = (Action<Sandbox>)methodInfo.CreateDelegate(typeof(Action<Sandbox>));
		}

		public override object Invoke(Sandbox sb, object[] args)
		{
			_func(sb);
			return null;
		}
	}
}