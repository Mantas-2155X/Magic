using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

namespace UI
{
	public class Fade : MonoBehaviour
	{
		private static Fade instance;
		public static Fade Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Fade UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					UnityEngine.Debug.LogError("[Fade] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);

				instance = copy.GetComponent<Fade>();
				return instance;
			}
		}

		[SerializeField]
		public Image Image;

		public void SetAlpha(float alpha)
		{
			var color = Image.color;
			color.a = alpha;

			Image.color = color;
		}
	}
}