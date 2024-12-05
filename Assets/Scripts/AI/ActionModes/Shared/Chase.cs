using AI.Enums;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Chase
	{
		private readonly float stopAt;

		public Chase(float stopAt)
		{
			this.stopAt = stopAt;
		}

		private float currentStopAt;

		/// <summary>
		/// Has the npc try to chase and reach the specified target
		/// This only does one step of trying and should be placed in Update
		/// Returns true if the npc has chased and reached the target
		/// </summary>
		public bool ChaseTarget(NPC npc, Transform target)
		{
			var agent = npc.Agent;
			var transform = npc.transform;
			
			// Try to stop at this distance
			var currentStopTarget = currentStopAt + agent.stoppingDistance;
			
			// NPC not within target range, keep walking
			if (Vector3.Distance(target.position, transform.position) > currentStopTarget)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(target.position, npc.Destination) <= currentStopAt + agent.stoppingDistance)
				{
					if (npc.AIMode != EAIMode.Walking)
						npc.Walk(target.position);

					return false;
				}
				
				// Target moved away from destination range, reset the path
				if (currentStopAt < stopAt)
					ResetCurrentStopAt();

				npc.Walk(target.position);
				return false;
			}

			// Within range but can't see the target, reduce the stop range to walk closer to the target
			if (!npc.HasSightOf(target, stopAt + agent.stoppingDistance))
			{
				currentStopAt /= 1.2f;

				if (npc.AIMode != EAIMode.Walking)
					npc.Walk(target.position);
				
				return false;
			}

			ResetCurrentStopAt();
			
			// Performing jump, stay on walking state until thats done
			if (agent.isOnOffMeshLink)
				return false;
				
			// Reached walking range
			return true;
		}
		
		/// <summary>
		/// Reset the potentially reduced stop range back to the initial value
		/// </summary>
		public void ResetCurrentStopAt()
		{
			currentStopAt = stopAt;
		}
	}
}