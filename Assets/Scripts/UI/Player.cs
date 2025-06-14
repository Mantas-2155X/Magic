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

				var copy = Addressables.InstantiateAsync("Assets/Prefabs/UI/Player UI.prefab").WaitForCompletion();
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

		[SerializeField]
		public Notice Notice;
	}
}