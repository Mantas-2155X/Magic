using ScriptableObjects;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class AimAt
	{
		private readonly NPC owner;

		public AimAt(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Rotates the npc towards the target with the specified degrees range
		/// This only does one step of aiming and should be placed in Update
		/// Returns true if angle between npc and target is within specified maximum angle
		/// </summary>
		public bool AimTowards(Transform target)
		{
			if (owner.Paralyzed)
				return false;
			
			var npcData = (NPCData)owner.Data;
			var rb = owner.Body.Rigidbody;
			
			var targetPosition = target.position - rb.position;
			targetPosition.y = 0;

			var targetRotation = Quaternion.LookRotation(targetPosition, owner.GetTransform().up);

			if (owner.Agent.IsNavMesh)
			{
				rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, npcData.RotationSpeed * Time.deltaTime));
			}
			else
			{
				var pos1 = target.position;
				var pos2 = rb.position;
			
				var verticalDistance = pos1.y - pos2.y;
			
				pos1.y = 0;
				pos2.y = 0;
				
				// Target is below npc and same horizontal position
				if (verticalDistance < 0 && (pos1 - pos2).magnitude < 1f)
					return true;
			}
			
			return Quaternion.Angle(rb.rotation, targetRotation) < npcData.AimAngle;
		}
	}
}