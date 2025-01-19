using System;
using Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace UI
{
	public class Title : MonoBehaviour
	{
		public static Title Instance;

		[SerializeField]
		public InputActionReference TitleAction;

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

		#region MonoBehaviour

		public void Awake()
		{
			Instance = this;
			
			DontDestroyOnLoad(gameObject);
			
			updateButtons();

			var action = TitleAction.action;
			action.performed += onTitlePerformed;
			action.Enable();
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
			if (state == isActiveAndEnabled)
				return;
			
			gameObject.SetActive(state);

			if (state)
			{
				var spellbook = Spellbook.Spellbook.Instance;
				if (spellbook != null)
					spellbook.Display(false);

				var aiManager = AIManager.Instance;
				if (aiManager != null && aiManager.Player != null)
					aiManager.Player.DisableInput();
			}
			else
			{
				var player = AIManager.Instance.Player;
				if (player != null)
					player.EnableInput();
			}
		}
		
		#endregion

		#region Events

		public void OnNewGame()
		{
			Close();
			Addressables.LoadSceneAsync("Scenes/World3");
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
			throw new NotImplementedException();
		}

		public void OnReturnToTitle()
		{
			Close();
			Addressables.LoadSceneAsync("Scenes/Title");
		}
		
		public void OnQuitGame()
		{
			#if UNITY_EDITOR
				UnityEditor.EditorApplication.ExitPlaymode();
			#else
				Application.Quit();
			#endif
		}
		
		#endregion

		#region Input

		private void onTitlePerformed(InputAction.CallbackContext ctx)
		{
			if (SceneManager.GetActiveScene().name == "Title")
				return;
			
			Toggle();
		}

		#endregion
		
		private void updateButtons()
		{
			var inTitle = SceneManager.GetActiveScene().name == "Title";
			
			NewGameButton.SetActive(inTitle);
			ContinueButton.SetActive(!inTitle);
			LoadButton.SetActive(false);
			SaveButton.SetActive(false);
			SettingsButton.SetActive(false);
			ReturnToTitleButton.SetActive(!inTitle);
			QuitGameButton.SetActive(true);
		}
	}
}