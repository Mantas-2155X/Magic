using AI.Interfaces;
using Objects.Base;
using Objects.Enums;
using Weapons.Interfaces;

namespace Objects
{
	public class DroppedWeapon : BasePickupable
	{
		public override float PickupableAfter => 0.5f;
		public override EDestroyType DestroyAfterPickup => EDestroyType.Component;
		
		public IWeapon Weapon { get; private set; }

		public override void OnEnable()
		{
			Weapon = GetComponent<IWeapon>();
			DisplayName = Weapon.GetType().Name;
			
			base.OnEnable();
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