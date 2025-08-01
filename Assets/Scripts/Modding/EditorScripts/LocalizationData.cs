#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Modding.EditorScripts
{
	[Serializable]
	public class LocalizationData
	{
		[SerializeField]
		public string Language;
			
		[SerializeField]
		public List<LocalizationDataEntry> Entries;
	}
}
#endif