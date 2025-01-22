using System;
using Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using SceneManager = Managers.SceneManager;

namespace UI
{
	public class Title : MonoBehaviour
	{
		private static Title instance;
		public static Title Instance
		{
			get
			{
				if (instance != null)
					return instance;

				var prefab = Addressables.LoadAssetAsync<GameObject>("Assets/Prefabs/UI/Title UI.prefab").WaitForCompletion();
				if (prefab == null)
				{
					UnityEngine.Debug.LogError("[Title] Failed to load base prefab");
					return null;
				}

				var copy = Instantiate(prefab);
				DontDestroyOnLoad(copy);

				instance = copy.GetComponent<Title>();
				return instance;
			}
		}

		[SerializeField]
		public InputActionReference TitleAction;

		[SerializeField]
		public InputActionReference ConsoleAction;

		[SerializeField]
		public GameObject NewGameButton;		
		[SerializeField]
		public GameObject ContinueButton;
		[SerializeField]
		public GameObject LoadButton;
		[SerializeField]
		public GameObject SaveButton;
		[SerializeField]
		public GameObject SettingsButton;
		[SerializeField]
		public GameObject ReturnToTitleButton;
		[SerializeField]
		public GameObject QuitGameButton;

		[SerializeField]
		public Console Console;

		[SerializeField]
		public Settings Settings;
		
		#region MonoBehaviour

		public void Awake()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneChanged;
			
			updateButtons();

			var titleAction = TitleAction.action;
			titleAction.performed += onTitlePerformed;
			titleAction.Enable();
			
			var consoleAction = ConsoleAction.action;
			consoleAction.performed += onConsolePerformed;
			consoleAction.Enable();
		}

		#endregion
		
		#region Toggle

		public void Open()
		{
			Toggle(true);
		}

		public void Close()
		{
			Toggle(false);
		}

		public void Toggle()
		{
			Toggle(!isActiveAndEnabled);
		}
		
		public void Toggle(bool state)
		{
			gameObject.SetActive(state);

			if (state)
			{
				var spellbook = Spellbook.Spellbook.Instance;
				if (spellbook != null)
					spellbook.Display(false);

				var aiManager = AIManager.Instance;
				if (aiManager != null && aiManager.Player != null)
					aiManager.Player.DisableInput();
				
				updateButtons();
			}
			else
			{
				var player = AIManager.Instance.Player;
				if (player != null && player.IsAlive)
					player.EnableInput();
			}
		}
		
		#endregion

		#region Events

		public void OnNewGame()
		{
			SceneManager.Instance.ChangeScene("World3", true, true, true);
		}

		public void OnContinue()
		{
			Close();
		}

		public void OnLoad()
		{
			throw new NotImplementedException();
		}

		public void OnSave()
		{
			throw new NotImplementedException();
		}

		public void OnSettings()
		{
			if (Settings == null)
				return;
			
			Settings.Toggle();
		}

		public void OnReturnToTitle()
		{
			SceneManager.Instance.ChangeScene("Title", true, true, false);
		}
		
		public void OnQuitGame()
		{
			SceneManager.Instance.ChangeScene("Exit", true, false, false);
		}
		
		#endregion

		#region Input

		private void onTitlePerformed(InputAction.CallbackContext ctx)
		{
			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title")
				return;
			
			Toggle();
		}
		
		private void onConsolePerformed(InputAction.CallbackContext ctx)
		{
			if (Console == null)
				return;
			
			Console.Toggle();
		}

		#endregion
		
		private void updateButtons()
		{
			var inTitle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title";
			
			NewGameButton.SetActive(inTitle);
			ContinueButton.SetActive(!inTitle);
			LoadButton.SetActive(false);
			SaveButton.SetActive(false);
			SettingsButton.SetActive(true);
			ReturnToTitleButton.SetActive(!inTitle);
			QuitGameButton.SetActive(true);
		}
		
		private void onSceneChanged(Scene scene, LoadSceneMode mode)
		{
			if (!isActiveAndEnabled)
				return;
			
			updateButtons();
		}
	}
}