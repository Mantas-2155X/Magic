using System;
using System.Collections.Generic;
using Modding.Infos;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Modding
{
	public class Mod
	{
		public ModInfo Info { get; private set; }
		
		public string Directory { get; private set; }
		
		public Tuple<string, IResourceLocator> Catalog { get; set; }

		public List<string> Addresses { get; private set; }
		
		public bool CustomAssemblyLoaded { get; set; }

		public Mod(ModInfo info, string directory, string catalogPath)
		{
			Info = info;
			Directory = directory;
			Catalog = new Tuple<string, IResourceLocator>(catalogPath, null);
			Addresses = new List<string>();
			CustomAssemblyLoaded = false;
		}
	}
}