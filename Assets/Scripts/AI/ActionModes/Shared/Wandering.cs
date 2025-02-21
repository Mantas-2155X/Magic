using AI.Enums;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.AI;

namespace AI.ActionModes.Shared
{
	public class Wandering
	{
		private readonly NPC owner;

		public Wandering(NPC owner)
		{
			this.owner = owner;
		}

		/// <summary>
		/// Has the npc walk randomly by adding a random circle range to the current position and setting that as the destination
		/// Setting force to true will set this immediately, otherwise it will be ignored if the last time walk state was exited is less than WanderEvery 
		/// </summary>
		public void WalkRandomly(bool force, bool navCast = true, bool vertical = false)
		{
			if (!force && Time.time < owner.AIModes[EAIMode.Walking].LastExited + ((NPCData)owner.Data).WanderEvery)
				return;
			
			var pos = owner.Body.Rigidbody.position;
			
			var circle = Random.insideUnitSphere;
			circle.x *= Random.Range(owner.Agent.stoppingDistance, 15f);
			circle.y *= Random.Range(owner.Agent.stoppingDistance, 15f);
			circle.z *= Random.Range(owner.Agent.stoppingDistance, 15f);

			if (!vertical)
				circle.y = 0f;

			var target = new Vector3(pos.x + circle.x, pos.y + circle.y, pos.z + circle.z);

			if (navCast)
			{
				// Prevent wandering picking a destination that's cutting a navmesh
				if (NavMesh.Raycast(pos, target, out _, NavMesh.AllAreas))
					return;
			}
			else
			{
				// If hit something, use that as the point to prevent going through it
				if (Physics.Raycast(pos, (target - pos).normalized, out var hit, float.MaxValue, ~LayerMaskTools.GetMask()))
					target = hit.point;
			}
			
			owner.Walk(target);
		}
	}
}