using AI.Enums;
using ScriptableObjects;
using UnityEngine;

namespace AI.ActionModes.Shared
{
	public class Wander
	{
		private readonly NPC owner;

		public Wander(NPC owner)
		{
			this.owner = owner;
		}

		/// <summary>
		/// Has the npc walk randomly by adding a random circle range to the current position and setting that as the destination
		/// Setting force to true will set this immediately, otherwise it will be ignored if the last time walk state was exited is less than WanderEvery 
		/// </summary>
		public void WalkRandomly(bool force)
		{
			if (!force && Time.time < owner.AIModes[EAIMode.Walking].LastExited + ((NPCData)owner.Data).WanderEvery)
				return;
			
			var pos = owner.Body.Rigidbody.position;
			
			var circle = Random.insideUnitCircle;
			circle.x *= Random.Range(owner.Agent.stoppingDistance, 15f);
			circle.y *= Random.Range(owner.Agent.stoppingDistance, 15f);

			var target = new Vector3(pos.x + circle.x, pos.y, pos.z + circle.y);
			owner.Walk(target);
		}
	}
}