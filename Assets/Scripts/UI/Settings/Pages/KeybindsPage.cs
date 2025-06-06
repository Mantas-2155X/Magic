using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
		
		public override void Select(bool state)
		{
			base.Select(state);
			
			if (Items.Count == 0)
				createItems();
			
			AutoSelect = Items.Count == 0 ? Tab.gameObject : Items[0].RebindButton.gameObject;
			
			setupItems();
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
					rebindingItem = item;
					onRebindClicked();
				});

				item.KeybindText = item.RebindButton.transform.Find("KeybindText").GetComponent<TextMeshProUGUI>();
				item.Setting = pair.Key;

				var keybind = settingsManager.GetKeybind(pair.Key);
				
				item.InputAction = keybind.Item1;
				item.BindingIndex = keybind.Item2;
				
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
				return;
			}

			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];

				var nav = new Navigation();
				nav.mode = Navigation.Mode.Explicit;
				
				if (i == 0)
				{
					nav.selectOnUp = Tab;
					nav.selectOnDown = Items[i + 1].RebindButton;
				}
				else if (i == Items.Count - 1)
				{
					nav.selectOnUp = Items[i - 1].RebindButton;
					nav.selectOnDown = Items[0].RebindButton;
				}
				else
				{
					nav.selectOnUp = Items[i - 1].RebindButton;
					nav.selectOnDown = Items[i + 1].RebindButton;
				}

				item.RebindButton.navigation = nav;
			}
		}

		private void setupItems()
		{
			var settingsManager = SettingsManager.Instance;
			
			for (var i = 0; i < Items.Count; i++)
			{
				var item = Items[i];
				
				var binding = new InputBinding(settingsManager.GetString(item.Setting));
				item.KeybindText.text = binding.ToDisplayString();
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

				for (var k = 0; k < Items.Count; k++)
				{
					if (i == k)
						continue;
					
					var innerItem = Items[k];
					if (innerItem.KeybindText.text != item.KeybindText.text)
						continue;

					item.KeybindText.color = Color.red;
					innerItem.KeybindText.color = Color.red;
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
			
			rebindingItem.KeybindText.fontStyle = FontStyles.Italic;
			
			rebindingOperation = rebindingItem.InputAction.PerformInteractiveRebinding(rebindingItem.BindingIndex)
				.WithControlsExcluding("Gamepad")
				.WithControlsExcluding("Joystick")
				.WithControlsExcluding("Pointer")
				.WithControlsExcluding("<keyboard>/anyKey")
				.WithControlsExcluding("<keyboard>/enter")
				.WithCancelingThrough("<Keyboard>/escape");
			
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

			SettingsManager.Instance.SetSetting(rebindingItem.Setting, rebindingItem.InputAction.bindings[rebindingItem.BindingIndex].effectivePath);
			SelectionManager.Instance.SetSelection(rebindingItem.RebindButton.gameObject);
			
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
			
			SelectionManager.Instance.SetSelection(rebindingItem.RebindButton.gameObject);
			
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
			public TMP_Text KeybindText;

			[SerializeField]
			public string Setting;

			[SerializeField]
			public InputAction InputAction;

			[SerializeField]
			public int BindingIndex;
		}
	}
}