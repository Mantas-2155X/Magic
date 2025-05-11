using Newtonsoft.Json;

namespace State
{
	public class DecalCreateData : CreateData
	{
		[JsonProperty]
		public string AttachObjectID;
		
		[JsonProperty]
		public float NormalizedTime;

		[JsonProperty]
		public float ElapsedTime;
	}
}