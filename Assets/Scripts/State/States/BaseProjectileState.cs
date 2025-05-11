using Combat.Projectiles.Base;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class BaseProjectileState
	{
		[JsonProperty]
		public Vector3 StartingPosition;
				
		public static BaseProjectileState Read(BaseProjectile baseProjectile)
		{
			if (baseProjectile == null)
				return null;

			return new BaseProjectileState
			{
				StartingPosition = baseProjectile.StartingPosition
			};
		}

		public static void Apply(BaseProjectile baseProjectile, BaseProjectileState state)
		{
			if (baseProjectile == null)
				return;

			baseProjectile.SetState(state.StartingPosition);
		}
	}
}