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
		/// Returns true if the distance between the npc and the target is less than the npcs sense range and spot range if needed
		/// </summary>
		public bool SenseDistanceCheck(Transform target, bool includeSpotRange, bool useSpellRange)
		{
			var npcData = (NPCData)owner.Data;
			var distance = Vector3.Distance(owner.GetTransform().position, target.position);

			var canSense = distance < npcData.SenseRange;

			if (useSpellRange && canSense && distance > owner.SpellRange)
				canSense = false;
			
			var canSpot = !includeSpotRange || distance < npcData.SpotRange;
			
			return canSense && canSpot;
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