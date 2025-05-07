using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Enums;

namespace State
{
	[JsonObject]
	public class CreateData
	{
		[JsonProperty]
		public ECreateType Type;

		[JsonProperty]
		public string Name;

		[JsonProperty]
		public Dictionary<string, JObject> States;
	}
}