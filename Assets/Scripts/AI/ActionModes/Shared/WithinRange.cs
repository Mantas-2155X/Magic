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

		private readonly NavMeshPath validPath = new ();
		
		/// <summary>
		/// Returns true if the distance between the npc and the target is less than the npcs sense range
		/// </summary>
		public bool SenseDistanceCheck(Transform target)
		{
			return Vector3.Distance(owner.Body.Rigidbody.position, target.position) < ((NPCData)owner.Data).SenseRange;
		}

		/// <summary>
		/// Returns true if the agent has a path to this target
		/// </summary>
		public bool IsPathValid(Vector3 target)
		{
			validPath.ClearCorners();

			var agentState = owner.Agent.enabled;
			var hasPath = false;
			
			owner.ToggleAgent(true);

			if (owner.Agent.CalculatePath(target, validPath) && validPath.status == NavMeshPathStatus.PathComplete)
				hasPath = true;
			
			owner.ToggleAgent(agentState);
			return hasPath;
		}
	}
}