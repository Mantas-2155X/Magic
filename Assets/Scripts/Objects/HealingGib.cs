using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class HealingGib : BaseUsable
	{
		public override bool DestroyAfterUse => true;

		[SerializeField]
		public int HealAmount = 10;
		
		public override bool Use(IAlive user)
		{
			var success = base.Use(user);
			if (!success)
				return false;

			user.Heal(HealAmount, this);
			return true;
		}
	}
}