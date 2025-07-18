using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Enums;

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
		public List<SaveItem> Items;

		[JsonObject]
		public class SaveItem
		{
			[JsonProperty]
			public ELoadType LoadType;
			
			[JsonProperty]
			public ELoadTiming LoadTiming;

			[JsonProperty]
			public string ObjectID;

			[JsonProperty]
			public Tuple<string, JObject> CreateData;

			[JsonProperty]
			public Dictionary<string, JObject> ModifyData;
			
			[JsonProperty]
			public Dictionary<string, JObject> AliveData;
		}
	}
}