using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Managers
{
	public class SceneManager
	{
		private static SceneManager instance;
		public static SceneManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new SceneManager();
				return instance;
			}
		}

		private readonly float fadeDuration = 0.3f;
		
		public void ChangeScene(string scene, bool fadeIn, bool fadeOut, bool closeTitle)
		{
			ChangeSceneAsync(scene, fadeIn, fadeOut, closeTitle).Forget();
		}
		
		public async UniTask ChangeSceneAsync(string scene, bool fadeIn, bool fadeOut, bool closeTitle)
		{
			if (fadeIn)
				await fade(true);

			if (scene == "Exit")
			{
#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
#else
				Application.Quit();
#endif
				return;
			}

			var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, $"Changing scene from {currentScene} to {scene}");
			
			var handle = Addressables.LoadSceneAsync(scene);

			await UniTask.WaitUntil(() => handle.IsDone);

			if (closeTitle)
			{
				var title = Title.Instance;
				if (title != null) 
					title.Close();
			}
			
			if (fadeOut)
				await fade(false);
		}

		private async UniTask fade(bool fadeIn)
		{
			var fade = Fade.Instance;
			if (fade != null)
			{
				fade.SetAlpha(fadeIn ? 0f : 1f);
				fade.gameObject.SetActive(true);
			}
			
			var normalizedTime = 0f;
			while (normalizedTime < 1f)
			{
				await UniTask.NextFrame();
				
				fade = Fade.Instance;
				if (fade != null)
				{
					float value;

					if (fadeIn)
						value = normalizedTime;
					else
						value = 1f - normalizedTime;

					fade.SetAlpha(value);
				}
				
				normalizedTime += Time.unscaledDeltaTime / fadeDuration;
			}

			if (!fadeIn)
			{
				fade = Fade.Instance;
				if (fade != null)
				{
					fade.SetAlpha(1f);
					fade.gameObject.SetActive(false);
				}
			}
		}
	}
}