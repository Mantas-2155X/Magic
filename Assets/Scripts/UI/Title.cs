using System;
using System.Collections.Generic;
using Managers;
using UI.Settings.Pages;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
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
				WeakInstance = instance;
				return instance;
			}
		}

		public static Title WeakInstance;
		
		[SerializeField]
		public InputActionReference TitleAction;

		[SerializeField]
		public List<Button> Buttons; // 0 - newgame, 1 - continue, 2 - load, 3 - save, 4 - settings, 5 - returntotitle, 6 - quitgame, 7 - sceneselect
		[SerializeField]
		public List<GameObject> ButtonObjects;

		[SerializeField]
		public Console Console;

		[SerializeField]
		public Settings.Settings Settings;

		[SerializeField]
		public SceneSelect SceneSelect;
		
		[SerializeField]
		public GameObject Blocker;

		#region MonoBehaviour

		public void Awake()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneChanged;
			
			UpdateButtons();

			var titleAction = TitleAction.action;
			titleAction.performed += onTitle;
			titleAction.Enable();
			
			var consoleAction = SettingsManager.Instance.GetKeybind("keybinds-debug-console").Item1;
			consoleAction.performed += onConsole;
			consoleAction.Enable();
		}

		public void OnEnable()
		{
			if (SceneManager.Instance.GetCurrentScene() != "Title")
				PauseManager.Instance.Pause();
			
			var feature = RenderManager.Instance.BlurFeature;
			if (feature == null)
				return;
			
			feature.SetActive(true);
		}

		public void OnDisable()
		{
			PauseManager.Instance.Unpause();
			
			var feature = RenderManager.Instance.BlurFeature;
			if (feature == null)
				return;
			
			feature.SetActive(false);
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
				var playerUI = Player.Instance;
				if (playerUI != null)
					playerUI.HUD.Spellbook.Display(false);

				var aiManager = AIManager.Instance;
				if (aiManager != null && aiManager.Player != null)
					aiManager.Player.DisableInput();
				
				UpdateButtons();
				Select();
			}
			else
			{
				var player = AIManager.Instance.Player;
				if (player != null && player.IsAlive)
					player.EnableInput();
				
				Deselect();
			}
		}
		
		#endregion

		#region Events

		public void OnNewGame()
		{
			SceneManager.Instance.ChangeScene("World3", true, true, true);
		}

		public void OnSceneSelect()
		{
			if (SceneSelect == null)
				return;
			
			SceneSelect.Toggle();
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

		private void onTitle(InputAction.CallbackContext ctx)
		{
			if (KeybindsPage.IsRebinding || UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title")
				return;
			
			Toggle();
		}
		
		private void onConsole(InputAction.CallbackContext ctx)
		{
			if (KeybindsPage.IsRebinding || Console == null)
				return;
			
			Console.Toggle();
		}

		#endregion
		
		public void UpdateButtons()
		{
			var inTitle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title";
			
			ButtonObjects[0].SetActive(inTitle);
			ButtonObjects[1].SetActive(!inTitle);
			ButtonObjects[2].SetActive(false);
			ButtonObjects[3].SetActive(false);
			ButtonObjects[4].SetActive(true);
			ButtonObjects[5].SetActive(!inTitle);
			ButtonObjects[6].SetActive(true);
			ButtonObjects[7].SetActive(true);
			
			UpdateNavigation();
		}

		public void UpdateNavigation()
		{
			// Having the console or settings open might navigate to them since the mode is automatic vertical
			// Set up explicit nav for top and bottom buttons to prevent that from happening
			
			var basicNav = new Navigation
			{
				mode = Navigation.Mode.Vertical,
				wrapAround = true
			};
			
			var newGameButton = Buttons[0];
			var continueButton = Buttons[1];
			var quitGameButton = Buttons[6];

			var shouldExplicit = Console.isActiveAndEnabled || Settings.isActiveAndEnabled || SceneSelect.isActiveAndEnabled;
			if (shouldExplicit)
			{
				Button topButton;
				Button otherTopButton;
				
				if (ButtonObjects[0].activeSelf)
				{
					topButton = newGameButton;
					otherTopButton = continueButton;
				}
				else
				{
					otherTopButton = newGameButton;
					topButton = continueButton;
				}

				// Restore basic nav to the previous top button
				otherTopButton.navigation = basicNav;
				
				// Top button should nav to bottom button if going up
				var topNav = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = quitGameButton,
					selectOnDown = topButton.FindSelectable(topButton.transform.rotation * Vector3.down)
				};

				topButton.navigation = topNav;

				// Bottom button should nav to top button if going down
				var botNav = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = quitGameButton.FindSelectable(quitGameButton.transform.rotation * Vector3.up),
					selectOnDown = topButton
				};

				quitGameButton.navigation = botNav;
			}
			else
			{
				// Restore any nav changes
				newGameButton.navigation = basicNav;
				continueButton.navigation = basicNav;
				quitGameButton.navigation = basicNav;
			}
		}

		public void Select(bool withCondition = false)
		{
			if (withCondition)
			{
				if (Console.isActiveAndEnabled)
				{
					Console.Select();
					return;
				}
				
				if (Settings.isActiveAndEnabled)
				{
					Settings.Select();
					return;
				}
				
				if (SceneSelect.isActiveAndEnabled)
				{
					SceneSelect.Select();
					return;
				}
			}

			SelectionManager.Instance.SetSelection(ButtonObjects[0].activeSelf ? ButtonObjects[0] : ButtonObjects[1]);
		}

		public void Deselect(bool withCondition = false)
		{
			if (withCondition)
			{
				if (Console.isActiveAndEnabled)
					return;
				
				if (Settings.isActiveAndEnabled)
					return;
				
				if (SceneSelect.isActiveAndEnabled)
					return;
			}
			
			SelectionManager.Instance.SetSelection(null);
		}
		
		private void onSceneChanged(Scene scene, LoadSceneMode mode)
		{
			if (!isActiveAndEnabled)
				return;
			
			UpdateButtons();
		}
	}
}