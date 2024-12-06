using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Spin
	{
		private readonly NPC owner;

		public Spin(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Endlessly rotates the transform with the specified degrees range
		/// This only does one step of spinning and should be placed in Update
		/// </summary>
		public void SpinEndlessly(Transform transform)
		{
			transform.Rotate(transform.up, Random.Range(owner.RotationSpeed.x, owner.RotationSpeed.y) * Time.deltaTime);
		}
	}
}