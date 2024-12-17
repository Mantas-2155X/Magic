using Tools;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class HasSight
	{
		private readonly NPC owner;

		public HasSight(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Returns true if the npc has a clear raycast hit to the specified target
		/// </summary>
		public bool SightCheck(Transform transform, Transform target, float maxDistance)
		{
			if (target == null)
				return false;
			
			var direction = (target.position - transform.position).normalized;
			var ray = new Ray(transform.position + transform.up * 0.5f, direction);

			if (!Physics.Raycast(ray, out var hit, maxDistance, ~LayerMaskTools.GetMask()))
				return false;
			
			return hit.collider.transform == target;
		}
	}
}