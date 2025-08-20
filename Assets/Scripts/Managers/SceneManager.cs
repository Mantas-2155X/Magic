using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers.Events;
using Modding;
using Modding.Infos;
using ScriptableObjects;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

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

				if (instance.CurrentSceneIndex == -1)
				{
					var findScene = $"SCENE_{UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToUpper()}_NAME";

					for (var i = 0; i < instance.sceneDatas.Count; i++)
					{
						var sceneData = instance.sceneDatas[i];
						if (sceneData.Name != findScene)
							continue;

						instance.CurrentSceneIndex = i;
						break;
					}

					if (instance.CurrentSceneIndex == -1)
						Debug.LogError("[SceneManager] Could not find active scene");
				}
				
				return instance;
			}
		}

		public int CurrentSceneIndex { get; private set; } = -1;

		public static OnPreSceneLoadEvent OnPreSceneLoadEvent = new ();
		public static OnPostSceneLoadEvent OnPostSceneLoadEvent = new ();
		
		private readonly List<SceneData> sceneDatas = new ();
		
		public SceneData GetCurrentSceneData()
		{
			return sceneDatas[CurrentSceneIndex];
		}
		
		public List<SceneData> GetSceneDatas()
		{
			return sceneDatas;
		}
		
		public bool SceneExists(SceneData scene)
		{
			return sceneDatas.Contains(scene);
		}
		
		public bool IsInTitle()
		{
			return GetCurrentSceneData().Name == "SCENE_TITLE_NAME";
		}
		
		public void QuitGame()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExitPlaymode();
#else
			Application.Quit();
#endif
		}
		
		public void ReloadScene(bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f, bool waitForGI = true)
		{
			ReloadSceneAsync(fadeIn, fadeOut, closeTitle, fadeDuration, waitForGI).Forget();
		}
		
		public async UniTask ReloadSceneAsync(bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f, bool waitForGI = true)
		{
			await ChangeSceneAsync(GetCurrentSceneData(), fadeIn, fadeOut, closeTitle, fadeDuration, waitForGI);
		}
		
		public void ChangeScene(SceneData scene, bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f, bool waitForGI = true)
		{
			ChangeSceneAsync(scene, fadeIn, fadeOut, closeTitle, fadeDuration, waitForGI).Forget();
		}
		
		public async UniTask ChangeSceneAsync(SceneData scene, bool fadeIn, bool fadeOut, bool closeTitle, float fadeDuration = 0.3f, bool waitForGI = true)
		{
			if (fadeIn)
			{
				SelectionManager.Instance.SetSelection(null);
				await fade(true, fadeDuration);
			}

			var title = Title.Instance;
			title.CloseWindows();
			
			Debug.Log($"[SceneManager] Changing scene from {GetCurrentSceneData().LocalizedName} to {scene.LocalizedName}");
			PauseManager.Instance.Unpause();
			
			OnPreSceneLoadEvent?.Invoke(scene);

			CurrentSceneIndex = sceneDatas.IndexOf(scene);
			
			var previousTransformFunction = Addressables.InternalIdTransformFunc;
			setupTransformFunction(scene);
			
			var handle = Addressables.LoadSceneAsync(scene.Addressable.RuntimeKey, LoadSceneMode.Single, false);
			await UniTask.WaitUntil(() => handle.Status == AsyncOperationStatus.Succeeded);

			Addressables.InternalIdTransformFunc = previousTransformFunction;
			
			await handle.Result.ActivateAsync();
			await UniTask.WaitUntil(() => handle.IsDone);
			
			if (waitForGI)
				await UniTask.WaitForSeconds(0.2f, true);

			// Something inside the serializer is slow when ran for the first time. Warm it up during loading screens
			
			try
			{
				StateManager.Instance.Save(out _, false, true);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[SceneManager] Failed prewarming state save, {e}");
			}
			
			OnPostSceneLoadEvent?.Invoke(scene);

			if (closeTitle)
				title.Close();
			
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
			var availableScenes = ObjectManager.Instance.GetAllDatas<SceneData>();
			for (var i = 0; i < availableScenes.Count; i++)
			{
				var sceneData = availableScenes[i];
				if (sceneData.Name != "SCENE_SPLASH_NAME")
				{
					var location = Addressables.LoadResourceLocationsAsync(sceneData.Addressable.RuntimeKey).WaitForCompletion()[0];

					var key = location.PrimaryKey;
					if (!key.StartsWith("Scenes/"))
						continue;
				}

				sceneDatas.Add(sceneData);
			}
		}

		private ModInfo getSceneModInfo(SceneData sceneData)
		{
			var modInfos = ModLoader.Instance.GetModInfos();
			
			for (var i = 0; i < modInfos.Count; i++)
			{
				var modInfo = modInfos[i];
				var addresses = ModLoader.Instance.GetAddresses(modInfo);
				
				for (var k = 0; k < addresses.Count; k++)
				{
					var address = addresses[k];
					
					if (!address.StartsWith("Scenes/") || address[7..] != sceneData.Name)
						continue;

					return modInfo;
				}
			}

			return null;
		}

		private void setupTransformFunction(SceneData sceneData)
		{
			var modInfo = getSceneModInfo(sceneData);
			if (modInfo == null)
				return;

			var platform = "";

			switch (Application.platform)
			{
				case RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor:
					platform = "StandaloneLinux64";
					break;
				case RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor:
					platform = "StandaloneWindows64";
					break;
			}

			Addressables.InternalIdTransformFunc = location => !location.InternalId.StartsWith(platform) ? location.InternalId : $"{ModLoader.Instance.GetDirectory(modInfo)}/{location.InternalId}";
		}
	}
}