using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace State.Interfaces
{
	public interface ISaveable : IIdentifiable
	{
		public bool ShouldSave { get; }
		
		public Dictionary<string, JObject> Save();
		
		public void Load(Dictionary<string, JObject> data);
	}
}