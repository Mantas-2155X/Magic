using Components;
using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class TriggerState
	{
		[JsonProperty]
		public bool Triggered;
				
		[JsonProperty]
		public float? EnterTime;
		
		public static TriggerState Read(Trigger trigger)
		{
			if (trigger == null)
				return null;

			return new TriggerState
			{
				Triggered = trigger.Triggered,
			};
		}
		
		public static TriggerState Read(DelayedTrigger delayedTrigger)
		{
			if (delayedTrigger == null)
				return null;

			var state = new TriggerState
			{
				Triggered = delayedTrigger.Triggered
			};

			if (delayedTrigger.EnterCollider != null)
				state.EnterTime = Time.time - delayedTrigger.EnterTime;
			
			return state;
		}

		public static void Apply(Trigger trigger, TriggerState state)
		{
			if (trigger == null)
				return;

			trigger.SetState(state.Triggered);
		}
		
		public static void Apply(DelayedTrigger delayedTrigger, TriggerState state)
		{
			if (delayedTrigger == null)
				return;

			delayedTrigger.SetState(state.Triggered, state.EnterTime);
		}
	}
}