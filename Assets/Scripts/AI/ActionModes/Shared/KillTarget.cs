using AI.Enums;
using Tools;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class KillTarget
	{
		private readonly NPC owner;

		public KillTarget(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Aims at the target and shoots it
		/// </summary>
		public bool AimAndKill(Transform target, bool sightCheck, bool rangeCheck, bool spellRange)
		{
			if (owner.AIMode == EAIMode.Action)
			{
				if (rangeCheck)
				{
					// Make sure the target is within sense range
					if (!owner.WithinRange.SenseDistanceCheck(target, false, true))
						return false;
				}
				
				if (sightCheck)
				{
					// Make sure the target can be seen
					if (!owner.HasSight.SightCheck(target, true))
						return false;
				}
				
				// Turn towards the target and aim
				if (!owner.AimAt.AimTowards(target))
					return false;

				if (owner.Spell.NotNull())
					owner.Spell.StartCasting();
			}

			return true;
		}
	}
}