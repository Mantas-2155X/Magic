using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class KeybindsPage : SettingsPage
	{
		[SerializeField]
		public List<SKeybindsPageItem> Items = new ();

		[SerializeField]
		public GameObject Template;
		
		public static bool IsRebinding { get; private set; }

		private SKeybindsPageItem rebindingItem;
		private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
		private bool enableInputAction;
		private bool isKeyboard;
		private float lastRebindEnded;
		
		public override void Select(bool state)
		{
			base.Select(state);
			
			if (Items.Count == 0)
				createItems();
			
			if (Items.Count == 0)
			{
				AutoSelect = Tab.gameObject;
			}
			else
			{
				switch (Gamepad.all.Count)
				{
					case 0:
						AutoSelect = Items[0].RebindButton.gameObject;
						break;
					case > 0:
						AutoSelect = Items[0].ControllerRebindButton.gameObject;
						break;
				}
			}
			
			setupItems();
		}
		
		public override void ResetTab()
		{
			var settings = SettingsManager.Instance.GetSettings();
			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("keybinds-"))
					continue;
				
				SettingsManager.Instance.DefaultSetting(pair.Key);
			}
			
			base.ResetTab();
		}

		public void OnDisable()
		{
			if (!IsRebinding || rebindingOperation == null)
				return;
			
			rebindingOperation.Cancel();
		}

		private void createItems()
		{
			var settingsManager = SettingsManager.Instance;
			var settings = settingsManager.GetSettings();
			
			var parent = Template.transform.parent;

			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("keybinds"))
					continue;
				
				var copy = Instantiate(Template, parent).transform;

				var item = new SKeybindsPageItem();
				item.Localizer = copy.Find("Localizer").GetComponent<Localizer>();
				item.Localizer.Key = pair.Value.Name;
				item.Localizer.Apply();
				
				item.RebindButton = copy.Find("RebindButton").GetComponent<Button>();
				item.RebindButton.onClick.AddListener(delegate
				{
					if (Time.unscaledTime - lastRebindEnded < 0.15f)
						return;
					
					rebindingItem = item;
					isKeyboard = true;
					onRebindClicked();
				});
				
				item.ControllerRebindButton = copy.Find("RebindButton (Controller)").GetComponent<Button>();
				item.ControllerRebindButton.onClick.AddListener(delegate
				{
					if (item.ControllerBindingIndex == -1 || Time.unscaledTime - lastRebindEnded < 0.15f)
						return;
					
					rebindingItem = item;
					isKeyboard = false;
					onRebindClicked();
				});

				item.KeybindText = item.RebindButton.transform.Find("KeybindText").GetComponent<TextMeshProUGUI>();
				item.ControllerKeybindText = item.ControllerRebindButton.transform.Find("KeybindText").GetComponent<TextMeshProUGUI>();
				
				item.Setting = pair.Key;

				var keybind = settingsManager.GetKeybind(pair.Key);
				
				item.InputAction = keybind.Item1;
				item.BindingIndex = keybind.Item2;
				item.ControllerBindingIndex = keybind.Item3;
				
				copy.gameObject.SetActive(true);
				Items.Add(item);
			}
			
			setupNavigation();
		}

		private void setupNavigation()
		{
			if (Items.Count == 0)
				return;
			
			if (Items.Count == 1)
			{
				var item = Items[0];
				
				var nav = new Navigation();
				nav.mode = Navigation.Mode.Explicit;
				nav.selectOnUp = Tab;
				nav.selectOnDown = Tab;

				item.RebindButton.navigation = nav;
				item.ControllerRebindButton.navigation = nav;
				return;
			}

			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];

				var keyboardNav = new Navigation();
				keyboardNav.mode = Navigation.Mode.Explicit;
				
				var controllerNav = new Navigation();
				controllerNav.mode = Navigation.Mode.Explicit;
				
				if (i == 0)
				{
					keyboardNav.selectOnUp = Tab;
					keyboardNav.selectOnDown = Items[i + 1].RebindButton;
					
					controllerNav.selectOnUp = Tab;
					controllerNav.selectOnDown = Items[i + 1].ControllerRebindButton;
				}
				else if (i == Items.Count - 1)
				{
					keyboardNav.selectOnUp = Items[i - 1].RebindButton;
					keyboardNav.selectOnDown = Items[0].RebindButton;
					
					controllerNav.selectOnUp = Items[i - 1].ControllerRebindButton;
					controllerNav.selectOnDown = Items[0].ControllerRebindButton;
				}
				else
				{
					keyboardNav.selectOnUp = Items[i - 1].RebindButton;
					keyboardNav.selectOnDown = Items[i + 1].RebindButton;
					
					controllerNav.selectOnUp = Items[i - 1].ControllerRebindButton;
					controllerNav.selectOnDown = Items[i + 1].ControllerRebindButton;
				}

				item.RebindButton.navigation = keyboardNav;
				item.ControllerRebindButton.navigation = controllerNav;
			}
		}

		private void setupItems()
		{
			var settingsManager = SettingsManager.Instance;
			
			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];
				
				var path = settingsManager.GetString(item.Setting);
				
				var split = path.Split(",");
				switch (split.Length)
				{
					case 1:
						item.KeybindText.text = new InputBinding(split[0]).ToDisplayString();
						break;
					case 2:
						item.KeybindText.text = new InputBinding(split[0]).ToDisplayString();
						item.ControllerKeybindText.text = new InputBinding(split[1]).ToDisplayString();
						break;
				}
			}
			
			showDuplicates();
		}

		private void showDuplicates()
		{
			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];
				
				item.KeybindText.color = Color.black;
				item.KeybindText.fontStyle = FontStyles.Normal;

				item.ControllerKeybindText.color = Color.black;
				item.ControllerKeybindText.fontStyle = FontStyles.Normal;

				for (var k = 0; k < Items.Count; k++)
				{
					if (i == k)
						continue;
					
					var innerItem = Items[k];
					
					if (innerItem.KeybindText.text == item.KeybindText.text)
					{
						item.KeybindText.color = Color.red;
						innerItem.KeybindText.color = Color.red;
					}
					
					if (innerItem.ControllerKeybindText.text == item.ControllerKeybindText.text && item.ControllerBindingIndex != -1)
					{
						item.ControllerKeybindText.color = Color.red;
						innerItem.ControllerKeybindText.color = Color.red;
					}
				}
			}
		}

		private void onRebindClicked()
		{
			if (IsRebinding)
				return;
			
			var blocker = Title.Instance.Blocker;
			blocker.SetActive(true);
			
			SelectionManager.Instance.SetSelection(blocker);
			
			enableInputAction = rebindingItem.InputAction.enabled;
			rebindingItem.InputAction.Disable();
			
			if (isKeyboard)
			{
				rebindingItem.KeybindText.fontStyle = FontStyles.Italic;

				rebindingOperation = rebindingItem.InputAction.PerformInteractiveRebinding(rebindingItem.BindingIndex)
					.WithExpectedControlType<ButtonControl>()
					.WithControlsExcluding("Gamepad")
					.WithControlsExcluding("Joystick")
					.WithControlsExcluding("Pointer")
					.WithControlsExcluding("<keyboard>/anyKey")
					.WithControlsExcluding("<keyboard>/enter")
					.WithControlsExcluding("<Keyboard>/escape")
					.WithCancelingThrough("<Keyboard>/escape");
			}
			else
			{
				rebindingItem.ControllerKeybindText.fontStyle = FontStyles.Italic;

				rebindingOperation = rebindingItem.InputAction.PerformInteractiveRebinding(rebindingItem.ControllerBindingIndex)
					.WithExpectedControlType<ButtonControl>()
					.WithControlsExcluding("Keyboard")
					.WithControlsExcluding("Joystick")
					.WithControlsExcluding("Pointer")
					.WithControlsExcluding("<Gamepad>/start")
					.WithCancelingThrough("<Gamepad>/start");
			}
			
			rebindingOperation.OnComplete(onRebindComplete);
			rebindingOperation.OnCancel(onRebindCanceled);
			
			IsRebinding = true;
			
			rebindingOperation.Start();
		}

		private void onRebindComplete(InputActionRebindingExtensions.RebindingOperation operation)
		{
			if (!IsRebinding)
				return;
			
			IsRebinding = false;
			
			if (enableInputAction)
				rebindingItem.InputAction.Enable();
			
			operation.Dispose();
			
			Title.Instance.Blocker.SetActive(false);

			var bindings = rebindingItem.InputAction.bindings;
			
			var keyboardPath = bindings[rebindingItem.BindingIndex].effectivePath;
			var actualKeybind = rebindingItem.ControllerBindingIndex == -1 ? keyboardPath : $"{keyboardPath},{bindings[rebindingItem.ControllerBindingIndex].effectivePath}";
			
			SettingsManager.Instance.SetSetting(rebindingItem.Setting, actualKeybind);
			SelectionManager.Instance.SetSelection(isKeyboard ? rebindingItem.RebindButton.gameObject : rebindingItem.ControllerRebindButton.gameObject);

			lastRebindEnded = Time.unscaledTime;
			setupItems();
		}
		
		private void onRebindCanceled(InputActionRebindingExtensions.RebindingOperation operation)
		{
			if (!IsRebinding)
				return;

			IsRebinding = false;
			
			if (enableInputAction)
				rebindingItem.InputAction.Enable();

			operation.Dispose();
			
			Title.Instance.Blocker.SetActive(false);
			
			SelectionManager.Instance.SetSelection(isKeyboard ? rebindingItem.RebindButton.gameObject : rebindingItem.ControllerRebindButton.gameObject);
			
			lastRebindEnded = Time.unscaledTime;
			setupItems();
		}

		[Serializable]
		public struct SKeybindsPageItem
		{
			[SerializeField]
			public Localizer Localizer;
			
			[SerializeField]
			public Button RebindButton;

			[SerializeField]
			public Button ControllerRebindButton;

			[SerializeField]
			public TMP_Text KeybindText;

			[SerializeField]
			public TMP_Text ControllerKeybindText;

			[SerializeField]
			public string Setting;

			[SerializeField]
			public InputAction InputAction;

			[SerializeField]
			public int BindingIndex;

			[SerializeField]
			public int ControllerBindingIndex;
		}
	}
}