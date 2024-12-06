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
		/// Rotates the transform towards the target with the specified degrees range
		/// This only does one step of aiming and should be placed in Update
		/// Returns true if angle between npc and target is within specified maximum angle
		/// </summary>
		public bool AimTowardsTarget(Transform transform, Transform target)
		{
			var targetPosition = target.position - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(owner.RotationSpeed.x, owner.RotationSpeed.y) * Time.deltaTime);
			
			return Quaternion.Angle(transform.rotation, targetRotation) < owner.AimAngle;
		}
	}
}