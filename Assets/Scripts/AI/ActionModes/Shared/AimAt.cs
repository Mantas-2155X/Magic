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
			
			var rb = owner.Body.Rigidbody;
			
			var targetPosition = target.position - rb.position;
			targetPosition.y = 0;

			var npcData = (NPCData)owner.Data;
			
			var targetRotation = Quaternion.LookRotation(targetPosition, owner.GetTransform().up);
			rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, npcData.RotationSpeed * Time.deltaTime));
			
			return Quaternion.Angle(rb.rotation, targetRotation) < npcData.AimAngle;
		}
	}
}