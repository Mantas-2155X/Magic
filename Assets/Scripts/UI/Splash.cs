using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace UI
{
	public class Splash : MonoBehaviour
	{
		public void Awake()
		{
			loadTitleAsync().Forget();
		}

		private async UniTask loadTitleAsync()
		{
			var handle = Addressables.LoadSceneAsync("Scenes/Title");
			
			await UniTask.WaitUntil(() => handle.IsDone);
			
			_ = Debug.Instance;
			_ = Title.Instance;
		}
	}
}