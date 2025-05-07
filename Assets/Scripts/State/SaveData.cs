using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace State
{
	[JsonObject]
	public class SaveData
	{
		[JsonProperty]
		public int FileVersion;
			
		[JsonProperty]
		public string Scene;

		[JsonProperty]
		public List<string> DestroyedObjects;

		[JsonProperty]
		public List<string> DestroyedComponents;
		
		[JsonProperty]
		public List<string> KilledAlives;

		[JsonProperty]
		public Dictionary<string, Tuple<string, Dictionary<string, JObject>>> Gibs;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Objects;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Alives;
	}
}