using AI.Interfaces;
using Objects.Base;
using Objects.Enums;
using UnityEngine;

namespace Objects
{
	public class Manapack : BasePickupable
	{
		public override float PickupableAfter => 0.5f;
		public override EDestroyType DestroyAfterPickup => EDestroyType.GameObject;

		[SerializeField]
		public int GenerateAmount = 25;

		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && user.CurrentMana < user.StartingMana;
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			user.GenerateMana(GenerateAmount, this);
			return true;
		}
	}
}