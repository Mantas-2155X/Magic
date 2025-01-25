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
			_ = SelectionManager.Instance;
			_ = ConsoleManager.Instance;
			_ = SettingsManager.Instance;
			_ = LocalizationManager.Instance;
			_ = GameManager.Instance;
			
			loadTitleAsync().Forget();
		}

		private async UniTask loadTitleAsync()
		{
			await SceneManager.Instance.ChangeSceneAsync("Title", true, true, false);
			
			_ = Debug.Instance;
			_ = Title.Instance;
		}
	}
}