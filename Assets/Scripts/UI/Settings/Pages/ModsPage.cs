using System;
using System.Collections.Generic;
using Managers;
using Modding;
using Modding.Infos;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class ModsPage : SettingsPage
	{
		[SerializeField]
		public List<SModsPageItem> Items = new ();

		[SerializeField]
		public GameObject Template;

		public override void Select(bool state)
		{
			base.Select(state);
			
			if (Items.Count == 0)
				createItems();

			if (Items.Count == 0 || !SceneManager.Instance.IsInTitle())
				AutoSelect = Tab.gameObject;
			else
				AutoSelect = Items[0].ToggleButton.transform.parent.gameObject;
			
			setupItems();
		}

		public void OnEnable()
		{
			if (gameObject.activeSelf)
				Select(true);
		}

		private void createItems()
		{
			var modInfos = ModLoader.Instance.GetModInfos();
			var parent = Template.transform.parent;

			for (var i = 0; i < modInfos.Count; i++)
			{
				var copy = Instantiate(Template, parent).transform;

				var item = new SModsPageItem();
				item.Title = copy.Find("Title").GetComponent<TMP_Text>();
				item.Version = copy.Find("Version").GetComponent<TMP_Text>();
				item.ToggleButton = copy.Find("Toggle").GetComponent<Button>();
				item.ToggleLocalizer = item.ToggleButton.GetComponentInChildren<Localizer>();

				var index = i;
				item.ToggleButton.onClick.AddListener(delegate
				{
					toggleMod(index);
				});

				copy.gameObject.SetActive(true);
				Items.Add(item);
			}

			setupNavigation();
		}

		private void setupItems()
		{
			var isTitle = SceneManager.Instance.IsInTitle();
			var modInfos = ModLoader.Instance.GetModInfos();
			
			for (var i = 0; i < Items.Count; i++)
			{
				var modInfo = modInfos[i];

				var item = Items[i];
				item.Title.text = modInfo.GetGUID();
				item.Version.text = modInfo.Version;
				item.ModInfo = modInfo;
				
				item.ToggleButton.interactable = isTitle;

				item.ToggleLocalizer.Key = modInfo.Disabled ? "SETTINGS_MODS_DISABLED" : "SETTINGS_MODS_ENABLED";
				item.ToggleLocalizer.Apply();
				
				Items[i] = item;
			}
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

				item.ToggleButton.navigation = nav;
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
					nav.selectOnDown = Items[i + 1].ToggleButton;
				}
				else if (i == Items.Count - 1)
				{
					nav.selectOnUp = Items[i - 1].ToggleButton;
					nav.selectOnDown = Items[0].ToggleButton;
				}
				else
				{
					nav.selectOnUp = Items[i - 1].ToggleButton;
					nav.selectOnDown = Items[i + 1].ToggleButton;
				}

				item.ToggleButton.navigation = nav;
			}
		}
		
		private void toggleMod(int index)
		{
			UnityEngine.Debug.LogWarning("[ModsPage] Toggling mods is disabled due to limitations. Edit the info file inside the mods directory instead");
			return;
			
			var item = Items[index];
			var modInfo = item.ModInfo;
			
			if (!SceneManager.Instance.IsInTitle())
				return;
			
			if (modInfo.Disabled)
				ModLoader.Instance.EnableMod(modInfo);
			else
				ModLoader.Instance.DisableMod(modInfo);
			
			setupItems();
		}
		
		[Serializable]
		public struct SModsPageItem
		{
			[SerializeField]
			public TMP_Text Title;
			
			[SerializeField]
			public TMP_Text Version;

			[SerializeField]
			public Button ToggleButton;

			[SerializeField]
			public Localizer ToggleLocalizer;

			[NonSerialized]
			public ModInfo ModInfo;
		}
	}
}