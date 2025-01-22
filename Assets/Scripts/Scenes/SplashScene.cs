using Cysharp.Threading.Tasks;
using Managers;
using UI;
using UnityEngine;
using Debug = UI.Debug;

namespace Scenes
{
	public class SplashScene : MonoBehaviour
	{
		public void Awake()
		{
			_ = LocalizationManager.Instance;
			_ = GameManager.Instance;
			
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