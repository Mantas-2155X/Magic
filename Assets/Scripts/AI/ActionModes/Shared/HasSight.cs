//#define DEBUG_SIGHT

using System.Collections.Generic;
using Objects.Interfaces;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.AI;

namespace AI.ActionModes.Shared
{
	public class HasSight
	{
		private readonly NPC owner;
		
		private readonly List<int> validColliders = new ();

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

			var data = (NPCData)owner.Data;
			
			var transform = owner.GetTransform();
			var position = transform.position;

			var direction = (target.position - position).normalized;
			var originCenter = owner.Body.Core.position;

			if (!data.UseSightCheck)
				return true;
			
			validColliders.Clear();
			
			var hitOrigin = originCenter;
			var breakableHit = false;

			Transform hitTransform = null;
			RaycastHit hit = default;
			
			while (hitTransform != target)
			{
				if (!Physics.Raycast(hitOrigin, direction, out hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
					return false;

				hitOrigin = hit.point + (direction * 0.1f);
				hitTransform = hit.transform;
				
				// Hit the target, count as fine
				if (hitTransform == target)
				{
					validColliders.Add(hit.colliderInstanceID);
					break;
				}
				
				var iObject = hitTransform.GetComponent<IObject>();
				if (iObject.IsNull())
					return false;
			
				if (!iObject.ObjectData.IsBreakable)
					return false;

				// Allow one breakable object to count as still visible
				if (breakableHit)
					return false;
				
				// Hit a breakable object, count as fine
				breakableHit = true;
				
				validColliders.Add(hit.colliderInstanceID);
			}
#if UNITY_EDITOR && DEBUG_SIGHT
			Debug.DrawLine(originCenter, direction * 50f, Color.magenta);
#endif

			// Projectiles have thickness so we need to cast more rays to make sure the projectile isn't going to just hit a wall. Spherecast does not work here because it spawns inside a collider and therefore it ignores the wall 
			if (extraCasts && data.UseExtraCasts)
			{
				var halfRadius = owner.Agent.IsNavMesh ? (NavMesh.GetSettingsByID(owner.Agent.NavMeshAgent.agentTypeID).agentRadius / 2f) : owner.Agent.Agent.Grid.Radius / 2f;
				
#if UNITY_EDITOR && DEBUG_SIGHT
			Debug.DrawLine(originCenter + transform.right * halfRadius, direction * 50f, Color.magenta);
			Debug.DrawLine(originCenter + -transform.right * halfRadius, direction * 50f, Color.magenta);
			Debug.DrawLine(originCenter + transform.up * halfRadius, direction * 50f, Color.magenta);
			Debug.DrawLine(originCenter + -transform.up * halfRadius, direction * 50f, Color.magenta);
#endif
				
				if (!Physics.SphereCast(transform.position, halfRadius, direction, out var extraHit, hit.distance + 1, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
					return false;

				if (!validColliders.Contains(extraHit.colliderInstanceID))
				{
					var iObject = extraHit.transform.GetComponent<IObject>();
					if (iObject.IsNull())
						return false;
			
					if (!iObject.ObjectData.IsBreakable)
						return false;
					
					// allow one breakable
					return true;
				}
			}
			
			return true;
		}
	}
}