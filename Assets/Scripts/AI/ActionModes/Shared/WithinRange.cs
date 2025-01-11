using ScriptableObjects;
using UnityEngine;
using UnityEngine.AI;

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
			return Vector3.Distance(owner.GetTransform().position, target.position) < ((NPCData)owner.Data).SenseRange;
		}

		/// <summary>
		/// Returns true if the angle between the npc and the target is within the field of view
		/// </summary>
		public bool FieldOfViewCheck(Transform target)
		{
			var ownerTr = owner.GetTransform();
			
			var direction = target.position - ownerTr.position;
			var angle = Vector3.Angle(direction, ownerTr.forward);
			
			return angle <= ((NPCData)owner.Data).FieldOfView;
		}
	}
}