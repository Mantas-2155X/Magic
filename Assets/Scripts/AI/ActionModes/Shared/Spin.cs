using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Spin
	{
		private readonly Vector2 degreesRange;

		public Spin(float minDegree, float maxDegree)
		{
			degreesRange = new Vector2(minDegree, maxDegree);
		}
		
		/// <summary>
		/// Endlessly rotates the transform with the specified degrees range
		/// This only does one step of spinning and should be placed in FixedUpdate
		/// </summary>
		public void SpinEndlessly(Transform transform)
		{
			transform.Rotate(transform.up, Random.Range(degreesRange.x, degreesRange.y));
		}
	}
}