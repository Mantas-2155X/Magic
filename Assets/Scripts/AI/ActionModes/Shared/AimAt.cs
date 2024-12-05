using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class AimAt
	{
		private readonly Vector2 degreesRange;
		private readonly float maximumAngle;

		public AimAt(float minDegree, float maxDegree, float maximumAngle)
		{
			degreesRange = new Vector2(minDegree, maxDegree);
			this.maximumAngle = maximumAngle;
		}
		
		/// <summary>
		/// Rotates the transform towards the target with the specified degrees range
		/// This only does one step of aiming and should be placed in FixedUpdate
		/// Returns true if angle between npc and target is within specified maximum angle
		/// </summary>
		public bool AimTowardsTarget(Transform transform, Transform target)
		{
			var targetPosition = target.position - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(degreesRange.x, degreesRange.y));
			
			return Quaternion.Angle(transform.rotation, targetRotation) < maximumAngle;
		}
	}
}