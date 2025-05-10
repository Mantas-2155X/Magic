using AI.Interfaces;
using Combat.Spells.Interfaces;
using Objects.Interfaces;
using UnityEngine;

namespace Tools
{
	public static class NullTools
	{
		public static bool IsNull(this IObject obj) => isNull(obj);
		public static bool IsNull(this IAlive obj) => isNull(obj);
		public static bool IsNull(this ISpell obj) => isNull(obj);

		private static bool isNull(object obj)
		{
			if (obj == null)
				return true;

			if (obj is Object unityObject && unityObject == null)
				return true;

			return false;
		}
	}
}