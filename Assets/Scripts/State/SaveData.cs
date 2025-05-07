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
		public Dictionary<string, Dictionary<Type, JObject>> Objects;
	}
}