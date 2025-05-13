using Newtonsoft.Json;
using Scenes;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class World4State
	{
		[JsonProperty]
		public float StartTimeElapsed;
		
		[JsonProperty]
		public bool TimerStopped;

		[JsonProperty]
		public float AttackEvery;
		
		[JsonProperty]
		public bool AttacksStarted;
		
		[JsonProperty]
		public float AttacksStartTimeElapsed;

		public static World4State Read(World4 world4)
		{
			if (world4 == null)
				return null;

			var time = Time.time;
			
			var state = new World4State
			{
				StartTimeElapsed = time - world4.StartTime,
				TimerStopped = world4.TimerStopped,
				AttackEvery = world4.AttackEvery,
				AttacksStarted = world4.AttacksStarted,
				AttacksStartTimeElapsed = world4.AttacksStarted ? time - world4.AttacksStartTime : 0f
			};

			return state;
		}

		public static void Apply(World4 world4, World4State state)
		{
			if (world4 == null)
				return;

			world4.SetState(state.StartTimeElapsed, state.TimerStopped, state.AttackEvery, state.AttacksStarted, state.AttacksStartTimeElapsed);
		}
	}
}