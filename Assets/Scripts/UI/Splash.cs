using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Splash : MonoBehaviour
	{
		public void Awake()
		{
			Addressables.LoadSceneAsync("Scenes/Title");
		}
	}
}