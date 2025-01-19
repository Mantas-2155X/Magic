using Cysharp.Threading.Tasks;
using Managers;
using UnityEngine;

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
			await SceneManager.Instance.ChangeSceneAsync("Scenes/Title", true, true, false);
			
			_ = Debug.Instance;
			_ = Title.Instance;
		}
	}
}