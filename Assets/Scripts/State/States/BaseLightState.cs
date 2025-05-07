using Newtonsoft.Json;
using Objects.Base;

namespace State.States
{
	[JsonObject]
	public class BaseLightState
	{
		[JsonProperty]
		public bool Enabled;
				
		public static BaseLightState Read(BaseLight baseLight)
		{
			if (baseLight == null)
				return null;

			return new BaseLightState
			{
				Enabled = baseLight.Enabled,
			};
		}

		public static void Apply(BaseLight baseLight, BaseLightState state)
		{
			if (baseLight == null)
				return;

			baseLight.Toggle(state.Enabled);
		}
	}
}