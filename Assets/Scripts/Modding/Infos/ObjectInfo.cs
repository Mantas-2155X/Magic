using Newtonsoft.Json;

namespace Modding.Infos
{
	[JsonObject]
	public class ObjectInfo
	{
		[JsonProperty]
		public string Type;
				
		[JsonProperty]
		public string Name;
	}
}