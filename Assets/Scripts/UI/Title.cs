using System;
using Managers;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
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
		public Settings.Settings Settings;
		
		#region MonoBehaviour

		public void Awake()
		{
			UnityEngine.SceneManagement.SceneManager.sceneLoaded += onSceneChanged;
			
			UpdateButtons();

			var titleAction = TitleAction.action;
			titleAction.performed += onTitle;
			titleAction.Enable();
			
			var consoleAction = ConsoleAction.action;
			consoleAction.performed += onConsole;
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
			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title")
				return;
			
			Toggle();
		}
		
		private void onConsole(InputAction.CallbackContext ctx)
		{
			if (Console == null)
				return;
			
			Console.Toggle();
		}

		#endregion
		
		// todo: turn the buttons into an array and use indexes instead
		public void UpdateButtons()
		{
			var inTitle = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Title";
			
			NewGameButton.SetActive(inTitle);
			ContinueButton.SetActive(!inTitle);
			LoadButton.SetActive(false);
			SaveButton.SetActive(false);
			SettingsButton.SetActive(true);
			ReturnToTitleButton.SetActive(!inTitle);
			QuitGameButton.SetActive(true);
			
			UpdateNavigation();
		}

		// todo: optimize, too many getcomponent calls
		public void UpdateNavigation()
		{
			// Having the console or settings open might navigate to them since the mode is automatic vertical
			// Set up explicit nav for top and bottom buttons to prevent that from happening
			
			var basicNav = new Navigation
			{
				mode = Navigation.Mode.Vertical,
				wrapAround = true
			};
			
			var shouldExplicit = Console.isActiveAndEnabled || Settings.isActiveAndEnabled;
			if (shouldExplicit)
			{
				var bottomButton = QuitGameButton.GetComponent<Button>();

				Button topButton;
				Button otherTopButton;
				
				if (NewGameButton.activeSelf)
				{
					topButton = NewGameButton.GetComponent<Button>();
					otherTopButton = ContinueButton.GetComponent<Button>();
				}
				else
				{
					otherTopButton = NewGameButton.GetComponent<Button>();
					topButton = ContinueButton.GetComponent<Button>();
				}

				// Restore basic nav to the previous top button
				otherTopButton.navigation = basicNav;
				
				// Top button should nav to bottom button if going up
				var topNav = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = bottomButton,
					selectOnDown = topButton.FindSelectable(topButton.transform.rotation * Vector3.down)
				};

				topButton.navigation = topNav;

				// Bottom button should nav to top button if going down
				var botNav = new Navigation
				{
					mode = Navigation.Mode.Explicit,
					selectOnUp = bottomButton.FindSelectable(bottomButton.transform.rotation * Vector3.up),
					selectOnDown = topButton
				};

				bottomButton.navigation = botNav;
			}
			else
			{
				// Restore any nav changes
				NewGameButton.GetComponent<Button>().navigation = basicNav;
				ContinueButton.GetComponent<Button>().navigation = basicNav;
				QuitGameButton.GetComponent<Button>().navigation = basicNav;
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
			}

			SelectionManager.Instance.SetSelection(NewGameButton.activeSelf ? NewGameButton : ContinueButton);
		}

		public void Deselect(bool withCondition = false)
		{
			if (withCondition)
			{
				if (Console.isActiveAndEnabled)
					return;
				
				if (Settings.isActiveAndEnabled)
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