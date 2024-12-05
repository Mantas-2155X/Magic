using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class HasSight
	{
		private readonly float range;
		
		public HasSight(float range)
		{
			this.range = range;
		}

		/// <summary>
		/// Returns true if the npc has a clear raycast hit to the specified target
		/// </summary>
		public bool SightCheck(NPC npc, Transform target)
		{
			return npc.HasSightOf(target, range);
		}
	}
}