using System.Collections.Generic;
using Newtonsoft.Json;

namespace Modding.Infos
{
	[JsonObject]
	public class LocalizationInfo
	{
		[JsonProperty]
		public string Language;

		[JsonProperty]
		public Dictionary<string, string> Entries;
	}
}