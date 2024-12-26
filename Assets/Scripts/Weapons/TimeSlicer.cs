using Attacks;
using Weapons.Base;

namespace Weapons
{
	public class TimeSlicer : BaseWeapon
	{
		public override bool FinishCasting()
		{
			if (TimeSlice.Active)
			{
				CancelCasting();
				return false;
			}
			
			return base.FinishCasting();
		}

		public override bool CanCast()
		{
			return base.CanCast() && !TimeSlice.Active;
		}
	}
}