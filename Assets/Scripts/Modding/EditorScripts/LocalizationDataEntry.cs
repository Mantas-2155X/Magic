#if UNITY_EDITOR
using System;
using UnityEngine;

namespace Modding.EditorScripts
{
	[Serializable]
	public class LocalizationDataEntry
	{
		[SerializeField]
		public string Name;
			
		[SerializeField]
		public string Description;
	}
}
#endif