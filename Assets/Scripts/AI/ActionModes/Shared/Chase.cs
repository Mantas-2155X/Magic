using AI.Enums;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Chase
	{
		private readonly NPC owner;

		public Chase(NPC owner)
		{
			this.owner = owner;
		}

		private float currentChaseRange;
		
		/// <summary>
		/// Has the npc try to chase and reach the specified target
		/// This only does one step of trying and should be placed in Update
		/// Returns true if the npc has chased and reached the target
		/// </summary>
		public bool ChaseCheck(Transform target)
		{
			var agent = owner.Agent;
			var transform = owner.GetTransform();
			
			// Try to stop at this distance
			var currentStopTarget = currentChaseRange + agent.stoppingDistance;
			
			// NPC not within target range, keep walking
			if (Vector3.Distance(target.position, transform.position) > currentStopTarget)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(target.position, owner.Destination) <= currentChaseRange + agent.stoppingDistance)
				{
					if (owner.AIMode != EAIMode.Walking)
						owner.Walk(target.position);

					return false;
				}
				
				// Target moved away from destination range, reset the path
				if (currentChaseRange < owner.GetSpellRange())
					ResetChaseRange(true);

				owner.Walk(target.position);
				return false;
			}

			// Within range but can't see the target, reduce the stop range to walk closer to the target
			if (!owner.HasSight.SightCheck(target))
			{
				currentChaseRange /= 1.2f;

				if (owner.AIMode != EAIMode.Walking)
					owner.Walk(target.position);
				
				return false;
			}

			// Set the chase range to the full actual value since we reached the target at a lowered rate, allowing micromovements of the target while staying in range
			ResetChaseRange(false);
			
			// Performing jump, stay on walking state until thats done
			if (agent.isOnOffMeshLink)
				return false;
				
			// Reached walking range
			return true;
		}
		
		/// <summary>
		/// Reset the potentially reduced chase range back to the initial value
		/// Setting lowered to true sets the range to be slightly smaller to prevent stuttering
		/// </summary>
		public void ResetChaseRange(bool lowered)
		{
			var spellRange = owner.GetSpellRange();
			currentChaseRange = lowered ? spellRange - 1.5f : spellRange;
		}
	}
}