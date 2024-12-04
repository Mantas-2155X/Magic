using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class HasSight
	{
		private readonly float maximumAngle;
		private readonly float range;
		
		public HasSight(float maximumAngle, float range)
		{
			this.maximumAngle = maximumAngle;
			this.range = range;
		}

		/// <summary>
		/// Returns true if the npc is facing the target look rotation within the specified maximum angle and has a clear raycast hit to the specified target
		/// </summary>
		public bool SightCheck(NPC npc, Transform target, Quaternion lookRotation)
		{
			return Quaternion.Angle(npc.transform.rotation, lookRotation) < maximumAngle && npc.HasSightOf(target, range);
		}
	}
}