using Newtonsoft.Json;

namespace State
{
	public class ProjectileCreateData : CreateData
	{
		[JsonProperty]
		public float Range;

		[JsonProperty]
		public string Attack;

		[JsonProperty]
		public string SourceObjectID;
	}
}