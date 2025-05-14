using Newtonsoft.Json;
using Scenes;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class World6State
	{
		public int CurrentWave;
		public int RemainingSpawners;
		
		public float WaveStartElapsed;
		
		public bool WorldStarted;
		public bool WorldEnded;

		public static World6State Read(World6 world6)
		{
			if (world6 == null)
				return null;
			
			var state = new World6State
			{
				CurrentWave = world6.CurrentWave,
				RemainingSpawners = world6.RemainingSpawners,
				WaveStartElapsed = world6.WorldStarted ? Time.time - world6.WaveStartTime : 0f,
				WorldStarted = world6.WorldStarted,
				WorldEnded = world6.WorldEnded
			};

			return state;
		}

		public static void Apply(World6 world6, World6State state)
		{
			if (world6 == null)
				return;

			world6.SetState(state.CurrentWave, state.RemainingSpawners, state.WaveStartElapsed, state.WorldStarted, state.WorldEnded);
		}
	}
}