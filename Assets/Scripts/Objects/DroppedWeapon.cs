using AI.Interfaces;
using Objects.Base;
using Objects.Enums;
using Weapons.Interfaces;

namespace Objects
{
	public class DroppedWeapon : BasePickupable
	{
		public override float PickupableAfter => 1f;
		public override EDestroyType DestroyAfterPickup => EDestroyType.Component;
		
		public IWeapon Weapon { get; private set; }

		public void Awake()
		{
			Weapon = GetComponent<IWeapon>();
		}
		
		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && user.Weapon?.GetType() != Weapon?.GetType();
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