using AI;
using Newtonsoft.Json;
using UI.Enums;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class NPCState
	{
		[JsonProperty]
		public bool SelfDestructed;

		[JsonProperty]
		public float SelfDestructElapsed;
		
		public static NPCState Read(NPC npc)
		{
			if (npc == null)
				return null;

			var state = new NPCState
			{
				SelfDestructed = npc.SelfDestructed,
				SelfDestructElapsed = Time.time - npc.SelfDestructStart
			};
			
			return state;
		}

		public static void Apply(NPC npc, NPCState state)
		{
			if (npc == null)
				return;

			npc.SetState(state.SelfDestructed, state.SelfDestructElapsed);
		}
	}
}