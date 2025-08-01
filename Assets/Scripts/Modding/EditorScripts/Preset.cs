#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Modding.EditorScripts
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
		public string CustomAssembly = "";

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
	}
}
#endif