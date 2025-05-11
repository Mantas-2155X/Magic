using Newtonsoft.Json;

namespace State
{
	public class AttackCreateData : CreateData
	{
		[JsonProperty]
		public string SourceObjectID;
		
		[JsonProperty]
		public string TargetObjectID;
		
		[JsonProperty]
		public float ElapsedTime;
	}
}