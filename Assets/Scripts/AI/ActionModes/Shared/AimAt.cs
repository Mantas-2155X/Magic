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

			if (owner.Flight == null)
				rb.MoveRotation(Quaternion.RotateTowards(rb.rotation, targetRotation, npcData.RotationSpeed * Time.deltaTime));
			else
				owner.Flight.AimAt(target);
			
			return Quaternion.Angle(rb.rotation, targetRotation) < npcData.AimAngle;
		}
	}
}