using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace State
{
	[JsonObject]
	public class CreateData
	{
		[JsonProperty]
		public string Name;

		[JsonProperty]
		public Dictionary<string, JObject> States;
	}
}