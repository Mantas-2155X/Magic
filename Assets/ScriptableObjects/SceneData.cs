using UnityEngine;
using UnityEngine.AddressableAssets;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class SceneData : Data
	{
		[Header("Scene")]
		[SerializeField]
		public AssetReference Addressable;
	}
}