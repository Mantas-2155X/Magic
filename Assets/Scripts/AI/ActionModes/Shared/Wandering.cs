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
		public void WalkRandomly(bool force)
		{
			if (!force && Time.time < owner.AIModes[EAIMode.Walking].LastExited + ((NPCData)owner.Data).WanderEvery)
				return;

			var pos = owner.Body.Rigidbody.position;
			
			var circle = Random.insideUnitSphere;
			circle.x *= Random.Range(owner.Agent.StoppingDistance, 15f);
			circle.y *= Random.Range(owner.Agent.StoppingDistance, 15f);
			circle.z *= Random.Range(owner.Agent.StoppingDistance, 15f);

			if (!owner.Agent.HasFlight)
				circle.y = 0f;

			var target = new Vector3(pos.x + circle.x, pos.y + circle.y, pos.z + circle.z);
			var direction = target - pos;

			if (owner.Agent.IsNavMesh)
			{
				// Prevent wandering picking a destination that's cutting a navmesh
				if (NavMesh.Raycast(pos, target, out var hit, NavMesh.AllAreas) && hit.distance <= direction.magnitude)
					target = hit.position + hit.normal;
			}
			else
			{
				// If hit something, use that as the point to prevent going through it
				if (Physics.Raycast(pos, direction.normalized, out var hit, direction.magnitude, ~LayerMaskTools.GetMask()))
					target = hit.point + hit.normal;
			}
			
			owner.Walk(target);
		}
	}
}