using Components;
using Newtonsoft.Json;

namespace State.States
{
	[JsonObject]
	public class TriggerState
	{
		[JsonProperty]
		public bool Triggered;
				
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

			return new TriggerState
			{
				Triggered = delayedTrigger.Triggered,
			};
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

			delayedTrigger.SetState(state.Triggered);
		}
	}
}