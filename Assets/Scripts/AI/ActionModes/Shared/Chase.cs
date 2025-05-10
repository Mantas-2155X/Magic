using AI.AIModes;
using AI.Enums;
using Tools;
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

		public float InterruptUntil;
		
		private float currentChaseRange;
		
		/// <summary>
		/// Chases the target, looks at it when reached and starts attacking it
		/// </summary>
		public bool ChaseAndKill(Transform target)
		{
			// Allow interrupting chasing so other actions can be done
			if (Time.time < InterruptUntil)
				return false;
			
			// Target within sense range, chase until it is reached
			if (!ChaseCheck(target))
				return false;

			// Reached target, stop walking and go into action
			if (owner.AIMode == EAIMode.Walking)
			{
				owner.ReturnAIMode();
				return false;
			}

			// Aim at target and fire
			return owner.KillTarget.AimAndKill(target, false, false, false);
		}
		
		/// <summary>
		/// Has the npc try to chase and reach the specified target
		/// This only does one step of trying and should be placed in Update
		/// Returns true if the npc has chased and reached the target
		/// </summary>
		public bool ChaseCheck(Transform target)
		{
			var agent = owner.Agent;
			var transform = owner.GetTransform();

			var targetPos = target.position;
			
			// Try to stop at this distance
			var currentStopTarget = currentChaseRange + agent.StoppingDistance;
			
			// NPC not within target range, keep walking
			if (Vector3.Distance(targetPos, transform.position) > currentStopTarget)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(targetPos, owner.Destination) <= currentChaseRange + agent.StoppingDistance)
				{
					if (owner.AIMode != EAIMode.Walking)
						owner.Walk(targetPos);

					return false;
				}
				
				// Target moved away from destination range, reset the path
				if (currentChaseRange < owner.SpellRange)
					ResetChaseRange(true);

				owner.Walk(targetPos);
				return false;
			}

			// Within range but can't see the target, reduce the stop range to walk closer to the target
			if (!owner.HasSight.SightCheck(target, true))
			{
				currentChaseRange /= 1.2f;

				if (owner.AIMode != EAIMode.Walking)
					owner.Walk(targetPos);
				
				return false;
			}

			// Set the chase range to the full actual value since we reached the target at a lowered rate, allowing micromovements of the target while staying in range
			ResetChaseRange(false);
			
			// Performing link, stay on walking state until thats done
			if (agent.IsOnOffMeshLink)
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
			currentChaseRange = lowered ? owner.SpellRange - 1.5f : owner.SpellRange;
		}
	}
}