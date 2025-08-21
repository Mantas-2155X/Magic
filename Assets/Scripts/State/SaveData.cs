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
		public bool AutoSave;

		[JsonProperty]
		public DateTimeOffset SavedTime;
		
		[JsonProperty]
		public List<string> DestroyedObjects;

		[JsonProperty]
		public List<string> DestroyedComponents;
		
		[JsonProperty]
		public List<string> KilledAlives;

		[JsonProperty]
		public Dictionary<string, SaveItem> Items;

		[JsonObject]
		public class SaveItem
		{
			[JsonProperty]
			public string OriginalScene;

			[JsonProperty]
			public string TransferredScene;
			
			[JsonProperty]
			public ELoadType LoadType;
			
			[JsonProperty]
			public ELoadTiming LoadTiming;

			[JsonProperty]
			public Tuple<string, JObject> CreateData;

			[JsonProperty]
			public Dictionary<string, JObject> ModifyData;
			
			[JsonProperty]
			public Dictionary<string, JObject> AliveData;
		}
	}

	[JsonObject]
	public class PartialSaveData
	{
		[JsonProperty]
		public string Scene;

		[JsonProperty]
		public bool AutoSave;

		[JsonProperty]
		public DateTimeOffset SavedTime;
	}
}