using System;
using System.Collections.Generic;
using Managers;
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
				AutoSelect = Items[0].Toggle.gameObject;
			
			setupItems();
		}

		public void OnEnable()
		{
			if (gameObject.activeSelf)
				Select(true);
		}

		private void createItems()
		{
			var mods = ObjectManager.Instance.Mods;
			var parent = Template.transform.parent;

			for (var i = 0; i < mods.Count; i++)
			{
				var copy = Instantiate(Template, parent).transform;

				var item = new SModsPageItem();
				item.Title = copy.Find("Title").GetComponent<TMP_Text>();
				item.Version = copy.Find("Version").GetComponent<TMP_Text>();
				item.Toggle = copy.Find("Left Toggle").GetComponent<Toggle>();

				var index = i;
				item.Toggle.onValueChanged.AddListener(delegate
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
			var mods = ObjectManager.Instance.Mods;
			
			for (var i = 0; i < Items.Count; i++)
			{
				var mod = mods[i];

				var item = Items[i];
				item.Title.text = $"{mod.Info.Author}.{mod.Info.Name}";
				item.Version.text = mod.Info.Version;
				
				item.Toggle.interactable = isTitle;
				item.Toggle.SetIsOnWithoutNotify(!mod.Info.Disabled);
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

				item.Toggle.navigation = nav;
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
					nav.selectOnDown = Items[i + 1].Toggle;
				}
				else if (i == Items.Count - 1)
				{
					nav.selectOnUp = Items[i - 1].Toggle;
					nav.selectOnDown = Items[0].Toggle;
				}
				else
				{
					nav.selectOnUp = Items[i - 1].Toggle;
					nav.selectOnDown = Items[i + 1].Toggle;
				}

				item.Toggle.navigation = nav;
			}
		}
		
		private void toggleMod(int index)
		{
			UnityEngine.Debug.LogWarning("[ModsPage] Toggling mods is disabled due to limitations. Edit the info file inside the mods directory instead");
			Items[index].Toggle.SetIsOnWithoutNotify(!ObjectManager.Instance.Mods[index].Info.Disabled);
			
			return;
			
			if (!SceneManager.Instance.IsInTitle())
				return;
			
			var mod = ObjectManager.Instance.Mods[index];

			if (mod.Info.Disabled)
				mod.Enable();
			else
				mod.Disable();
			
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
			public Toggle Toggle;
		}
	}
}