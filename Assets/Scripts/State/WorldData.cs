using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Enums;

namespace State
{
	[JsonObject]
	public class WorldData
	{
		[JsonProperty]
		public EWorldDataType Type;

		[JsonProperty]
		public Dictionary<string, JObject> States;
	}
}