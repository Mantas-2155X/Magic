using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using ScriptableObjects;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

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

		private readonly List<string> sceneNames = new ();
		private readonly List<SceneData> sceneDatas = new ();

		public string GetCurrentScene()
		{
			return UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
		}
		
		public List<string> GetSceneNames()
		{
			return sceneNames;
		}
		
		public List<SceneData> GetSceneDatas()
		{
			return sceneDatas;
		}
		
		public bool SceneExists(string scene)
		{
			return sceneNames.Contains(scene);
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
			
			PauseManager.Instance.Unpause();

			var handle = Addressables.LoadSceneAsync("Scenes/" + scene, LoadSceneMode.Single, false);
			await UniTask.WaitUntil(() => handle.Status == AsyncOperationStatus.Succeeded);
			await handle.Result.ActivateAsync();
			await UniTask.WaitUntil(() => handle.IsDone);
			await UniTask.WaitForSeconds(0.5f, true);

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
			
			fade.SetAlpha(fadeIn ? 0f : 1f);
			fade.gameObject.SetActive(true);
			
			var normalizedTime = 0f;
			while (normalizedTime < 1f)
			{
				await UniTask.NextFrame();
				
				float value;

				if (fadeIn)
					value = normalizedTime;
				else
					value = 1f - normalizedTime;

				fade.SetAlpha(value);
				
				normalizedTime += Time.unscaledDeltaTime / fadeDuration;
			}

			fade.SetAlpha(fadeIn ? 1f : 0f);
			
			if (!fadeIn)
				fade.gameObject.SetActive(false);
		}

		private void getScenes()
		{
			var availableScenes = ObjectManager.Instance.GetAllScenes();
			if (availableScenes == null || availableScenes.Length == 0)
				return;

			foreach (var sceneData in availableScenes)
			{
				var location = Addressables.LoadResourceLocationsAsync(sceneData.Addressable.RuntimeKey).WaitForCompletion()[0];
				
				var key = location.PrimaryKey;
				if (!key.StartsWith("Scenes/"))
					continue;

				var trimmed = key.Replace("Scenes/", "");
				if (trimmed == "")
					continue;
				
				sceneNames.Add(trimmed);
				sceneDatas.Add(sceneData);
			}
		}
	}
}