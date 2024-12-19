using AI.Interfaces;
using Objects.Base;
using Weapons.Interfaces;

namespace Objects
{
	public class DroppedWeapon : BaseObject
	{
		public IWeapon Weapon;

		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && user.Weapon?.WeaponData != Weapon?.WeaponData;
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			user.TakeWeapon(Weapon);
			return true;
		}
	}
}