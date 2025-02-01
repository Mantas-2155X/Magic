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
		
		public override void Select(bool state)
		{
			base.Select(state);
			
			if (Items.Count == 0)
				createItems();
			
			AutoSelect = Items.Count == 0 ? Tab.gameObject : Items[0].RebindButton.gameObject;
			
			setupItems();
		}

		private void createItems()
		{
			var parent = Template.transform.parent;
			var settings = SettingsManager.Instance.GetSettings();

			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("keybinds"))
					continue;
				
				var copy = Instantiate(Template, parent).transform;

				var item = new SKeybindsPageItem();
				item.Localizer = copy.Find("Localizer").GetComponent<Localizer>();
				item.RebindButton = copy.Find("RebindButton").GetComponent<Button>();
				item.KeybindText = item.RebindButton.transform.Find("KeybindText").GetComponent<TextMeshProUGUI>();
				item.Setting = pair.Key;

				item.Localizer.Key = pair.Value.Name;
				item.Localizer.Apply();
				
				item.RebindButton.onClick.AddListener(onRebindClicked);
				
				copy.gameObject.SetActive(true);
				Items.Add(item);
			}
			
			setupNavigation();
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
		}

		private void setupNavigation()
		{
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

		private void onRebindClicked()
		{
			
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
		}
	}
}