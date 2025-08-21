using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using State.Enums;

namespace State.Interfaces
{
	public interface ISaveable : IIdentifiable
	{
		public bool ShouldSave { get; }
		
		public bool ShouldTransfer { get; }
		
		public bool ExternallySpawned { get; set; }
		
		public string OriginalScene { get; set; }
		
		public string TransferredScene { get; set; }

		public ELoadType LoadType { get; }

		public ELoadTiming LoadTiming { get; }
		
		public JObject GetCreation();

		public static ISaveable ApplyCreation(Tuple<string, JObject> data) => throw new NotImplementedException();
		
		public Dictionary<string, JObject> GetModifications();
		
		public void ApplyModifications(Dictionary<string, JObject> data);
	}
}