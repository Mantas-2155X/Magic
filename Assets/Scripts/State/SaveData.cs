using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace State
{
	[JsonObject]
	public class SaveData
	{
		[JsonProperty]
		public string Scene;

		[JsonProperty]
		public List<string> DestroyedObjects;

		[JsonProperty]
		public List<string> DestroyedComponents;
		
		[JsonProperty]
		public List<string> KilledAlives;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Alives;

		[JsonProperty]
		public Dictionary<string, CreateData> Create;

		[JsonProperty]
		public Dictionary<string, WorldData> World;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Objects;
	}
}