using System;
using Managers;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class ControlsPage : SettingsPage
	{
		[SerializeField]
		public Localizer MouseSensitivityLocalizer;
		[SerializeField]
		public InputSlider MouseSensitivityInputSlider;
		
		[SerializeField]
		public Localizer ControllerSensitivityLocalizer;
		[SerializeField]
		public InputSlider ControllerSensitivityInputSlider;
		
		[SerializeField]
		public Localizer AllowHotbarScrollingLocalizer;
		[SerializeField]
		public Toggle AllowHotbarScrollingToggle;

		[SerializeField]
		public Localizer ShowSelectionLocalizer;
		[SerializeField]
		public Toggle ShowSelectionToggle;

		public override void Select(bool state)
		{
			base.Select(state);
			
			var mouseSensitivity = SettingsManager.Instance.GetSetting("controls-sensitivity-mouse");
			MouseSensitivityLocalizer.Key = mouseSensitivity.Name;
			MouseSensitivityLocalizer.Apply();
			
			MouseSensitivityInputSlider.SetValueWithoutNotify(Convert.ToSingle(mouseSensitivity.Value));
			
			var controllerSensitivity = SettingsManager.Instance.GetSetting("controls-sensitivity-controller");
			ControllerSensitivityLocalizer.Key = controllerSensitivity.Name;
			ControllerSensitivityLocalizer.Apply();
			
			ControllerSensitivityInputSlider.SetValueWithoutNotify(Convert.ToSingle(controllerSensitivity.Value));
			
			var allowHotbarScrolling = SettingsManager.Instance.GetSetting("controls-allowhotbarscrolling");
			AllowHotbarScrollingLocalizer.Key = allowHotbarScrolling.Name;
			AllowHotbarScrollingLocalizer.Apply();
			
			AllowHotbarScrollingToggle.SetIsOnWithoutNotify(Convert.ToBoolean(allowHotbarScrolling.Value));
			
			var showSelection = SettingsManager.Instance.GetSetting("controls-showselection");
			ShowSelectionLocalizer.Key = showSelection.Name;
			ShowSelectionLocalizer.Apply();
			
			ShowSelectionToggle.SetIsOnWithoutNotify(Convert.ToBoolean(showSelection.Value));
		}

		public override void ResetTab()
		{
			var settings = SettingsManager.Instance.GetSettings();
			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("controls-"))
					continue;
				
				SettingsManager.Instance.DefaultSetting(pair.Key);
			}
			
			base.ResetTab();
		}
		
		public void OnMouseSensitivityChanged(float value)
		{
			SettingsManager.Instance.SetSetting("controls-sensitivity-mouse", value);
		}
		
		public void OnControllerSensitivityChanged(float value)
		{
			SettingsManager.Instance.SetSetting("controls-sensitivity-controller", value);
		}

		public void OnAllowHotbarScrollingChanged(bool value)
		{
			SettingsManager.Instance.SetSetting("controls-allowhotbarscrolling", value);
		}
		
		public void OnShowSelectionChanged(bool value)
		{
			SettingsManager.Instance.SetSetting("controls-showselection", value);
		}
	}
}