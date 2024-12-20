using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class WithinRange
	{
		private readonly NPC owner;

		public WithinRange(NPC owner)
		{
			this.owner = owner;
		}
		
		/// <summary>
		/// Returns true if the distance between the npc and the target is less than the npcs sense range
		/// </summary>
		public bool SenseDistanceCheck(Transform target)
		{
			return Vector3.Distance(owner.Body.Rigidbody.position, target.position) < owner.SenseRange;
		}
	}
}