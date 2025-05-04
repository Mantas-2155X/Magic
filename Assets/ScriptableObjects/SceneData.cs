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

		[SerializeField]
		public bool Hidden;

		[SerializeField]
		public bool NoclipInitially;
		
		[SerializeField]
		public bool InvulnerableInitially;
		
		[SerializeField]
		public bool PowerfulInitially;
		
		[SerializeField]
		public bool FlashlightInitially;
	}
}