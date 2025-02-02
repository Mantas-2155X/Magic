using System.Collections.Generic;
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
				instance.getScenes();
				return instance;
			}
		}

		private readonly List<string> scenes = new ();

		public string GetCurrentScene()
		{
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		}
		
		public List<string> GetScenes()
		{
			return scenes;
		}
		
		public bool SceneExists(string scene)
		{
			return scenes.Contains(scene);
		}
		
		public void ReloadScene(bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f)
		{
			ReloadSceneAsync(fadeIn, fadeOut, closeTitle, fadeDuration).Forget();
		}
		
		public async UniTask ReloadSceneAsync(bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f)
		{
			await ChangeSceneAsync(GetCurrentScene(), fadeIn, fadeOut, closeTitle, fadeDuration);
		}
		
		public void ChangeScene(string scene, bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f)
		{
			ChangeSceneAsync(scene, fadeIn, fadeOut, closeTitle, fadeDuration).Forget();
		}
		
		public async UniTask ChangeSceneAsync(string scene, bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f)
		{
			if (fadeIn)
			{
				SelectionManager.Instance.SetSelection(null);
				await fade(true, fadeDuration);
			}

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
			UnityEngine.Debug.Log($"[SceneManager] Changing scene from {currentScene} to {scene}");
			
			var handle = Addressables.LoadSceneAsync("Scenes/" + scene);

			await UniTask.WaitUntil(() => handle.IsDone);

			if (closeTitle)
			{
				var title = Title.Instance;
				if (title != null) 
					title.Close();
			}
			
			if (fadeOut)
				await fade(false, fadeDuration);
		}

		private async UniTask fade(bool fadeIn, float fadeDuration)
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

		private void getScenes()
		{
			var locations = Addressables.LoadResourceLocationsAsync("scenes").WaitForCompletion();
			if (locations == null || locations.Count == 0)
				return;

			foreach (var location in locations)
			{
				var key = location.PrimaryKey;
				if (!key.StartsWith("Scenes/"))
					continue;

				var trimmed = key.Replace("Scenes/", "");
				if (trimmed == "")
					continue;
				
				scenes.Add(trimmed);
			}
			
			scenes.Sort();
		}
	}
}