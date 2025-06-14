using System;
using System.Collections.Generic;
using Managers;
using TMPro;
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

				var copy = Addressables.InstantiateAsync("Assets/Prefabs/UI/Title UI.prefab").WaitForCompletion();
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
		public List<Button> Buttons; // 0 - newgame, 1 - continue, 2 - saveload, 3 - unused, 4 - settings, 5 - returntotitle, 6 - quitgame, 7 - sceneselect
		[SerializeField]
		public List<GameObject> ButtonObjects;

		[SerializeField]
		public Console Console;

		[SerializeField]
		public Settings.Settings Settings;

		[SerializeField]
		public SceneSelect SceneSelect;
		
		[SerializeField]
		public SaveLoad SaveLoad;

		[SerializeField]
		public GameObject Blocker;

		[SerializeField]
		public Localizer CurrentScene;
		
		#region MonoBehaviour

		public void Awake()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneChanged;
			
			UpdateCurrentScene();
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

		public void OnDestroy()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded -= onSceneChanged;
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
				
				UpdateCurrentScene();
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

		public void CloseWindows(bool includeConsole = false)
		{
			if (includeConsole)
				Console.Display(false);
			
			Settings.Display(false);
			SceneSelect.Display(false);
			SaveLoad.Display(false);
		}
		
		#endregion

		#region Events

		public void OnNewGame()
		{
			throw new NotImplementedException();
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

		public void OnSaveLoad()
		{
			if (SaveLoad == null)
				return;
			
			SaveLoad.Toggle();
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
			if (KeybindsPage.IsRebinding || SceneManager.Instance.GetCurrentScene() == "Title")
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

		public void UpdateCurrentScene()
		{
			var scene = SceneManager.Instance.GetCurrentScene();
			
			CurrentScene.Key = $"SCENE_{scene.ToUpper()}_NAME";
			CurrentScene.Apply();
			
			CurrentScene.gameObject.SetActive(scene != "Title");
		}
		
		public void UpdateButtons()
		{
			var sceneManager = SceneManager.Instance;

			var inTitle = sceneManager.GetCurrentScene() == "Title";
			var currentSceneData = sceneManager.GetCurrentSceneData();
			
			ButtonObjects[0].SetActive(inTitle);
			ButtonObjects[1].SetActive(!inTitle);
			ButtonObjects[2].SetActive(true);
			ButtonObjects[4].SetActive(true);
			ButtonObjects[5].SetActive(!inTitle);
			ButtonObjects[6].SetActive(true);
			ButtonObjects[7].SetActive(inTitle);
			
			// needs main story
			Buttons[0].interactable = false;
			Buttons[0].GetComponentInChildren<TMP_Text>().color = new Color(0.75f, 0.75f, 0.75f);
			
			// only show saving for scenes that support it and for title
			Buttons[2].interactable = inTitle || currentSceneData != null && currentSceneData.SupportsSaving;
			Buttons[2].GetComponentInChildren<TMP_Text>().color = Buttons[2].interactable ? Color.white : new Color(0.75f, 0.75f, 0.75f);
			
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

			var shouldExplicit = Console.isActiveAndEnabled || Settings.isActiveAndEnabled || SceneSelect.isActiveAndEnabled || SaveLoad.isActiveAndEnabled;
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
				
				if (SaveLoad.isActiveAndEnabled)
				{
					SaveLoad.Select();
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
				
				if (SaveLoad.isActiveAndEnabled)
					return;
			}
			
			SelectionManager.Instance.SetSelection(null);
		}
		
		private void onSceneChanged(Scene scene, LoadSceneMode mode)
		{
			if (!isActiveAndEnabled)
				return;
			
			UpdateCurrentScene();
			UpdateButtons();
		}
	}
}