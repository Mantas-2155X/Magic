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
		public bool SightCheck(Transform target)
		{
			if (target == null)
				return false;
			
			var transform = owner.GetTransform();
			var rb = owner.Body.Rigidbody;

			var direction = (target.position - rb.position).normalized;
			var ray = new Ray(rb.position + transform.up * 0.5f, direction);

			if (!Physics.Raycast(ray, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
				return false;
			
			return hit.collider.transform == target;
		}
	}
}