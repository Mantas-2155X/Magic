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
		public static bool IsNull(this IObject obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IObject obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this IAlive obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IAlive obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this IAttack obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IAttack obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this ICast obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this ICast obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this IDecal obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IDecal obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this IProjectile obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IProjectile obj) => !isNull(obj);
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this ISpell obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this ISpell obj) => !isNull(obj);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsNull(this IWearable obj) => isNull(obj);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool NotNull(this IWearable obj) => !isNull(obj);

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