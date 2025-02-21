using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.AI;

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
			var position = transform.position;

			var direction = target.position - position;
			var originCenter = owner.Body.Core.position;

			if (!Physics.Raycast(originCenter, direction, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
				return false;

			// Check if the hit is the target so we don't waste extra casts calls
			var hitTransform = hit.collider.transform;
			if (hitTransform != target)
				return false;

			// Projectiles have thickness so we need to cast more rays to make sure the projectile isn't going to just hit a wall. Spherecast does not work here because it spawns inside a collider and therefore it ignores the wall 
			if (extraCasts && ((NPCData)owner.Data).UseExtraCasts)
			{
				var halfSize = NavMesh.GetSettingsByID(owner.Agent.agentTypeID).agentRadius / 2f;
				
				var instance = hit.colliderInstanceID;
				var originRight = originCenter + transform.right * halfSize;
				
				if (!Physics.Raycast(originRight, direction, out var hitRight, hit.distance + 1f, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore) || hitRight.colliderInstanceID != instance)
					return false;
				
				var originLeft = originCenter - transform.right * halfSize;
				
				if (!Physics.Raycast(originLeft, direction, out var hitLeft, hit.distance + 1f, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore) || hitLeft.colliderInstanceID != instance)
					return false;
			}
			
			return true;
		}
	}
}