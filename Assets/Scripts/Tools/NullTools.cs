using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Decals.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Combat.Wearables.Interfaces;
using Objects.Interfaces;
using UnityEngine;

namespace Tools
{
	public static class NullTools
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this object obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this object obj) => !isNull(obj);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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