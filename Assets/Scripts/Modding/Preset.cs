using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Modding
{
	[Serializable]
	public class Preset : ScriptableObject
	{
		[SerializeField]
		public string Author = "MyName";
		
		[SerializeField]
		public string Name = "ModName";

		[SerializeField]
		public string Version = "1.0.0";

		[SerializeField]
		public DefaultAsset CustomAssembly;

		[SerializeField]
		public List<Data> Objects = new ();

		[SerializeField]
		public List<LocalizationData> Localizations = new()
		{
			new LocalizationData
			{
				Language = "en",
				Entries = new List<LocalizationDataEntry>()
			}
		};

		public string CustomAssemblyPath
		{
			get
			{
				if (CustomAssembly == null)
					return "";

				if (!string.IsNullOrEmpty(customAssemblyPath) && CustomAssembly == previousCustomAssembly)
					return customAssemblyPath;

				customAssemblyPath = AssetDatabase.GetAssetPath(CustomAssembly);
				previousCustomAssembly = CustomAssembly;

				return customAssemblyPath;
			}
		}
		
		[NonSerialized]
		private DefaultAsset previousCustomAssembly;
		
		[NonSerialized]
		private string customAssemblyPath;
	}
}