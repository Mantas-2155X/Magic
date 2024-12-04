using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class AimAt
	{
		private readonly Vector2 degreesRange;

		public AimAt(float minDegree, float maxDegree)
		{
			degreesRange = new Vector2(minDegree, maxDegree);
		}
		
		/// <summary>
		/// Rotates the transform towards the target with the specified degrees range
		/// This only does one step of aiming and should be placed in FixedUpdate
		/// Returns the look rotation of the target that we're trying to reach
		/// </summary>
		public Quaternion AimStep(Transform transform, Transform target)
		{
			var targetPosition = target.position - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(degreesRange.x, degreesRange.y));
			
			return targetRotation;
		}
	}
}