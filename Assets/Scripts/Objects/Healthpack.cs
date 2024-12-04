using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class Healthpack : BasePickupable
	{
		[SerializeField]
		public int HealAmount = 25;

		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && user.CurrentHealth < user.StartingHealth;
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			user.Heal(HealAmount, this);
			return true;
		}
	}
}