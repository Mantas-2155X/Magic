using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class HealthGib : BaseObject
	{
		[SerializeField]
		public float HealAmount;

		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			user.RestoreHealth(HealAmount, this);
			return true;
		}
	}
}