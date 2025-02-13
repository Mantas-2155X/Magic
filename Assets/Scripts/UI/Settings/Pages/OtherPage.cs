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
		}

		public void OnCursorElementChanged(int value)
		{
			SettingsManager.Instance.SetSetting("other-cursorelement", value);
		}
		
		public void OnCursorSizeChanged(int value)
		{
			SettingsManager.Instance.SetSetting("other-cursorsize", value);
		}
	}
}