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
		public bool SightCheck(Transform target, bool extraCasts)
		{
			if (target == null)
				return false;
			
			var transform = owner.GetTransform();
			var position = owner.Body.Rigidbody.position;

			var direction = target.position - position;
			var originCenter = position + transform.up * 0.5f;

			if (!Physics.defaultPhysicsScene.Raycast(originCenter, direction, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
				return false;

			// Projectiles have thickness so we need to cast more rays to make sure the projectile isn't going to just hit a wall. Spherecast does not work here because it spawns inside a collider and therefore it ignores the wall 
			if (extraCasts)
			{
				var instance = hit.colliderInstanceID;
				var originRight = originCenter + transform.right * 0.4f;
				
				if (!Physics.defaultPhysicsScene.Raycast(originRight, direction, out var hitRight, hit.distance + 1f, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore) || hitRight.colliderInstanceID != instance)
					return false;
				
				var originLeft = originCenter - transform.right * 0.4f;
				
				if (!Physics.defaultPhysicsScene.Raycast(originLeft, direction, out var hitLeft, hit.distance + 1f, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore) || hitLeft.colliderInstanceID != instance)
					return false;
			}
			
			return hit.collider.transform == target;
		}
	}
}