using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class VideoPage : SettingsPage
	{
		[SerializeField]
		public Localizer ResolutionLocalizer;
		[SerializeField]
		public TMP_Dropdown ResolutionDropdown;

		[SerializeField]
		public Localizer FullscreenLocalizer;
		[SerializeField]
		public Toggle FullscreenToggle;
		
		private List<string> resolutions;
		
		public override void Select(bool state)
		{
			base.Select(state);
			
			resolutions = RenderManager.Instance.Resolutions;

			var resolution = SettingsManager.Instance.GetSetting("video-resolution");
			ResolutionLocalizer.Key = resolution.Name;
			ResolutionLocalizer.Apply();
			
			var options = ResolutionDropdown.options;
			ResolutionDropdown.ClearOptions();
			ResolutionDropdown.AddOptions(resolutions);

			var currentResolution = resolution.Value.ToString();
			for (var i = 0; i < options.Count; i++)
			{
				var option = options[i];
				if (option.text != currentResolution)
					continue;

				ResolutionDropdown.SetValueWithoutNotify(i);
				break;
			}

			var fullscreen = SettingsManager.Instance.GetSetting("video-fullscreen");
			FullscreenLocalizer.Key = fullscreen.Name;
			FullscreenLocalizer.Apply();
			
			FullscreenToggle.SetIsOnWithoutNotify(Convert.ToBoolean(fullscreen.Value));
		}

		public void OnResolutionChanged(int value)
		{
			if (resolutions == null || value >= resolutions.Count)
				return;
			
			SettingsManager.Instance.SetSetting("video-resolution", resolutions[value]);
		}

		public void OnFullscreenChanged(bool value)
		{
			SettingsManager.Instance.SetSetting("video-fullscreen", value);
		}
	}
}