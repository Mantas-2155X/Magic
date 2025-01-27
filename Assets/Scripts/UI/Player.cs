using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Player : MonoBehaviour
	{
		private static Player instance;
		public static Player Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Player UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					UnityEngine.Debug.LogError("[PlayerUI] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);

				instance = copy.GetComponent<Player>();
				WeakInstance = instance;
				return instance;
			}
		}

		public static Player WeakInstance;
		
		[SerializeField]
		public Stats Stats;
		
		[SerializeField]
		public Death Death;
		
		[SerializeField]
		public HUD HUD;
	}
}