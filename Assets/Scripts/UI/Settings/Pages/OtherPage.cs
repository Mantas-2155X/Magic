using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;

namespace UI.Settings.Pages
{
	public class OtherPage : SettingsPage
	{
		[SerializeField]
		public Localizer CursorElementLocalizer;
		[SerializeField]
		public DropdownLocalizer CursorElementDropdown;
		
		[SerializeField]
		public Localizer CursorSizeLocalizer;
		[SerializeField]
		public DropdownLocalizer CursorSizeDropdown;

		private readonly List<string> cursorElementKeys = new ()
		{
			"SETTINGS_DEFAULT",
			"SETTINGS_ELEMENT_ENERGY", 
			"SETTINGS_ELEMENT_HEALTH", 
			"SETTINGS_ELEMENT_MANA", 
			"SETTINGS_ELEMENT_FIRE", 
			"SETTINGS_ELEMENT_TIME",
			"SETTINGS_ELEMENT_LIGHTNING"
		};
		
		private readonly List<string> cursorSizeKeys = new ()
		{
			"SETTINGS_SIZE_TINY", 
			"SETTINGS_SIZE_SMALL", 
			"SETTINGS_SIZE_NORMAL", 
			"SETTINGS_SIZE_BIG", 
			"SETTINGS_SIZE_HUGE"
		};

		public override void Select(bool state)
		{
			base.Select(state);
			
			var cursorElement = SettingsManager.Instance.GetSetting("other-cursorelement");
			CursorElementLocalizer.Key = cursorElement.Name;
			CursorElementLocalizer.Apply();
			
			CursorElementDropdown.SetOptions(cursorElementKeys);
			CursorElementDropdown.SetValueWithoutNotify(Convert.ToInt32(cursorElement.Value));
			
			var cursorSize = SettingsManager.Instance.GetSetting("other-cursorsize");
			CursorSizeLocalizer.Key = cursorSize.Name;
			CursorSizeLocalizer.Apply();
			
			CursorSizeDropdown.SetOptions(cursorSizeKeys);
			CursorSizeDropdown.SetValueWithoutNotify(Convert.ToInt32(cursorSize.Value));

			updateSizeDropdown();
		}

		public void OnCursorElementChanged(int value)
		{
			SettingsManager.Instance.SetSetting("other-cursorelement", value);
			
			updateSizeDropdown();
		}
		
		public void OnCursorSizeChanged(int value)
		{
			SettingsManager.Instance.SetSetting("other-cursorsize", value);
		}

		private void updateSizeDropdown()
		{
			// TODO: Windows build doesn't change size, why?
			// Default cursor can't change size

			if (Application.platform == RuntimePlatform.WindowsPlayer || SettingsManager.Instance.GetInt("other-cursorelement") == 0)
			{
				CursorSizeDropdown.Dropdown.interactable = false;
				CursorSizeDropdown.Dropdown.image.color = Color.gray;
			}
			else
			{
				CursorSizeDropdown.Dropdown.interactable = true;
				CursorSizeDropdown.Dropdown.image.color = Color.black;
			}
		}
	}
}