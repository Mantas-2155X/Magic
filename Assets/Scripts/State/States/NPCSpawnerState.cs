using System.Collections.Generic;
using Newtonsoft.Json;
using Objects;

namespace State.States
{
	[JsonObject]
	public class NPCSpawnerState
	{
		[JsonProperty]
		public List<string> Spawned;
		
		[JsonProperty]
		public int Triggered;

		[JsonProperty]
		public bool Cleared;
		
		public static NPCSpawnerState Read(NPCSpawner npcSpawner)
		{
			if (npcSpawner == null)
				return null;

			var state = new NPCSpawnerState
			{
				Spawned = new List<string>()
			};

			foreach (var pair in npcSpawner.Spawned)
				state.Spawned.Add(pair.Key);

			state.Triggered = npcSpawner.Triggered;
			state.Cleared = npcSpawner.Cleared;
			
			return state;
		}

		public static void Apply(NPCSpawner npcSpawner, NPCSpawnerState state)
		{
			if (npcSpawner == null)
				return;

			npcSpawner.SetState(state.Spawned, state.Triggered, state.Cleared);
		}
	}
}