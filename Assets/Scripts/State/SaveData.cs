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
		public string Scene;

		[JsonProperty]
		public DateTimeOffset SavedTime;
		
		[JsonProperty]
		public List<string> DestroyedObjects;

		[JsonProperty]
		public List<string> DestroyedComponents;
		
		[JsonProperty]
		public List<string> KilledAlives;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Alives;

		[JsonProperty]
		public Dictionary<string, JObject> Create;

		[JsonProperty]
		public Dictionary<string, JObject> DeferredCreate;

		[JsonProperty]
		public Dictionary<string, JObject> World;
		
		[JsonProperty]
		public Dictionary<string, JObject> DeferredWorld;

		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> Objects;
		
		[JsonProperty]
		public Dictionary<string, Dictionary<string, JObject>> DeferredObjects;
	}
}