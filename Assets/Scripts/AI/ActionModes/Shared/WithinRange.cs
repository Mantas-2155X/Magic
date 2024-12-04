using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class WithinRange
	{
		private readonly float range;

		public WithinRange(float range)
		{
			this.range = range;
		}
		
		/// <summary>
		/// Returns true if the distance between provided transforms is less than the specified range
		/// </summary>
		public bool DistanceCheck(Transform transform, Transform target)
		{
			return Vector3.Distance(transform.position, target.position) < range;
		}
	}
}