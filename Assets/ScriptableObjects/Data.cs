using Managers;
using ScriptableObjects.Enums;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScriptableObjects
{
	public class Data : ScriptableObject
	{
		[Header("Basic")]
		[SerializeField]
		public string Name;
		
		[SerializeField]
		public string Description;

		[SerializeField]
		public Sprite Icon;
		
		[SerializeField]
		public ETag Tags;
		
		[Header("Instantiation")]
		[SerializeField]
		public AssetReference PrefabReference;
		
		[SerializeField]
		public string Type;
		
		[SerializeField]
		public string Assembly;

		public string LocalizedName => LocalizationManager.Instance.GetLocalizedEntry(Name);
		public string LocalizedDescription => LocalizationManager.Instance.GetLocalizedEntry(Description);
	}
}