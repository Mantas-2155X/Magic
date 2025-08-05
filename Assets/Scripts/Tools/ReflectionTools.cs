using System;
using System.Reflection;

namespace Tools
{
	public static class ReflectionTools
	{
		public static MethodInfo GetMethodDeep(Type type, string name, BindingFlags flags, Type stopAtType = null)
		{
			while (true)
			{
				var method = type.GetMethod(name, flags);
				if (method != null)
					return method;

				var baseType = type.BaseType;
				if (baseType == null || baseType == stopAtType)
					return null;

				type = baseType;
			}
		}
	}
}