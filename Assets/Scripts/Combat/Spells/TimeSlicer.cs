using Combat.Attacks;
using Combat.Spells.Base;

namespace Combat.Spells
{
	public class TimeSlicer : BaseSpell
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