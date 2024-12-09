using System;
using Weapons.Interfaces;

namespace Tools
{
	public static class TypeTools
	{
		public static Type FindType(string typeName)
		{
			var types = typeof(IWeapon).Assembly.GetTypes();
			foreach (var type in types)
			{
				if (type.Name == typeName)
					return type;
			}

			return null;
		}
	}
}