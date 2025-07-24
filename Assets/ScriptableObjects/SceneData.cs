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
		public bool Internal;

		[SerializeField]
		public bool Hidden;

		[SerializeField]
		public bool SupportsSaving;
		
		[SerializeField]
		public bool NoclipInitially;
		
		[SerializeField]
		public bool InvulnerableInitially;
		
		[SerializeField]
		public bool PowerfulInitially;
		
		[SerializeField]
		public bool FlashlightInitially;
		
		[SerializeField]
		public bool SpawnPlayer;

		[SerializeField]
		public bool ReloadOnPlayerDeath;
	}
}