using AI.Interfaces;
using Objects.Base;
using UnityEngine;
using Weapons.Interfaces;

namespace Objects
{
	public class DroppedWeapon : BasePickupable
	{
		[SerializeField]
		public string Weapon;
		
		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && user.Weapon?.GetType().Name != Weapon;
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			var weapon = Instantiate(Resources.Load<GameObject>($"Weapons/{Weapon}")).GetComponent<IWeapon>();
			user.TakeWeapon(weapon);
			
			return true;
		}
	}
}