using Newtonsoft.Json;
using Objects.Base;
using Objects.Enums;

namespace State.States
{
	[JsonObject]
	public class BaseDoorState
	{
		[JsonProperty]
		public EDoorState State;

		[JsonProperty]
		public bool Locked;

		[JsonProperty]
		public float Normalized;
				
		public static BaseDoorState Read(BaseDoor baseDoor)
		{
			if (baseDoor == null)
				return null;

			return new BaseDoorState
			{
				State = baseDoor.State,
				Locked = baseDoor.Locked,
				Normalized = baseDoor.Normalized
			};
		}

		public static void Apply(BaseDoor baseDoor, BaseDoorState state)
		{
			if (baseDoor == null)
				return;
			
			baseDoor.SetState(state.State, state.Normalized, state.Locked);
		}
	}
}