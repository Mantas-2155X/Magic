using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace State.Interfaces
{
	public interface ISaveable
	{
		public string ObjectID { get; set; }
		
		public Dictionary<string, JObject> Save();
		
		public void Load(Dictionary<string, JObject> data);
	}
}