using System.Collections.Generic;
using Newtonsoft.Json;
using Tools;
using World;

namespace State.States
{
	[JsonObject]
	public class WaterState
	{
		[JsonProperty]
		public List<string> AlivesObjectIDs;
		
		public static WaterState Read(Water water)
		{
			if (water == null)
				return null;

			var state = new WaterState();
			state.AlivesObjectIDs = new List<string>();

			for (var i = 0; i < water.Alives.Count; i++)
			{
				var alive = water.Alives[i];
				if (alive.IsNull() || !alive.IsAlive)
					continue;

				state.AlivesObjectIDs.Add(alive.ObjectID);
			}

			return state;
		}

		public static void Apply(Water water, WaterState state)
		{
			if (water == null)
				return;

			water.SetState(state.AlivesObjectIDs);
		}
	}
}