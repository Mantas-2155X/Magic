using AI.Interfaces;
using Combat.Wearables.Interfaces;
using Objects.Base;

namespace Objects
{
	public class DroppedWearable : BaseObject
	{
		public IWearable Wearable;

		public override bool CanPickup(IAlive user)
		{
			return base.CanPickup(user) && !user.HasWearable(Wearable.WearableData);
		}
		
		public override bool Pickup(IAlive user)
		{
			var success = base.Pickup(user);
			if (!success)
				return false;

			user.EquipWearable(Wearable);
			return true;
		}
	}
}