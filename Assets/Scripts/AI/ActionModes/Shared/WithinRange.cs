using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class WithinRange
	{
		private readonly NPC owner;

		public WithinRange(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Returns true if the distance between provided transforms is less than the specified range
		/// </summary>
		public bool DistanceCheck(Transform transform, Transform target)
		{
			return Vector3.Distance(transform.position, target.position) < owner.SenseRange;
		}
	}
}