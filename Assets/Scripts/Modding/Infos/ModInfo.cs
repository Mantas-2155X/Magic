using System.Collections.Generic;
using Modding.Enums;
using Newtonsoft.Json;

namespace Modding.Infos
{
	[JsonObject]
	public class ModInfo
	{
		[JsonProperty]
		public string Author;
			
		[JsonProperty]
		public string Name;
			
		[JsonProperty]
		public string Version;

		[JsonProperty]
		public bool Disabled;
			
		[JsonProperty]
		public bool UseCustomAssembly;

		[JsonProperty]
		public List<ObjectInfo> Objects;
			
		[JsonProperty]
		public List<LocalizationInfo> Localizations;
			
		public EModInfoValidity Validate()
		{
			if (string.IsNullOrWhiteSpace(Author))
				return EModInfoValidity.InvalidAuthor;

			if (string.IsNullOrWhiteSpace(Name))
				return EModInfoValidity.InvalidName;

			if (string.IsNullOrWhiteSpace(Version))
				return EModInfoValidity.InvalidVersion;

			if (Objects == null || Objects.Count == 0)
				return EModInfoValidity.NoObjects;

			return EModInfoValidity.Valid;
		}

		public string GetGUID()
		{
			return $"{Author}.{Name}";
		}
	}
}