using Newtonsoft.Json;
using Scenes;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class World7State
	{
		[JsonProperty]
		public bool OrbActivated;

		[JsonProperty]
		public bool PlayerIncluded;
		
		[JsonProperty]
		public float OrbSize;

		[JsonProperty]
		public float OrbLightBounceIntensity;

		[JsonProperty]
		public float ElapsedTime;
		
		public static World7State Read(World7 world7)
		{
			if (world7 == null)
				return null;

			var state = new World7State
			{
				OrbActivated = world7.ActivatedTime >= 0f,
				PlayerIncluded = world7.PlayerIncluded,
				OrbSize = world7.CurrentSize,
				OrbLightBounceIntensity = world7.CurrentBounceIntensity,
				ElapsedTime = world7.ActivatedTime >= 0f ? Time.time - world7.ActivatedTime : 0f
			};

			return state;
		}

		public static void Apply(World7 world7, World7State state)
		{
			if (world7 == null)
				return;

			world7.SetState(state.OrbActivated, state.PlayerIncluded, state.OrbSize, state.OrbLightBounceIntensity, state.ElapsedTime);
		}
	}
}