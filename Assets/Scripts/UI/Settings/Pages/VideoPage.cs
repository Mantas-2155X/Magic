using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UI.Elements;
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
		
		[SerializeField]
		public Localizer VSyncLocalizer;
		[SerializeField]
		public Toggle VSyncToggle;
		
		[SerializeField]
		public Localizer FPSLimitLocalizer;
		[SerializeField]
		public InputSlider FPSLimitInputSlider;
		
		[SerializeField]
		public Localizer RenderScaleLocalizer;
		[SerializeField]
		public InputSlider RenderScaleInputSlider;
		
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
			
			var vsync = SettingsManager.Instance.GetSetting("video-vsync");
			VSyncLocalizer.Key = vsync.Name;
			VSyncLocalizer.Apply();
			
			VSyncToggle.SetIsOnWithoutNotify(Convert.ToBoolean(vsync.Value));
			
			var fpsLimit = SettingsManager.Instance.GetSetting("video-fpslimit");
			FPSLimitLocalizer.Key = fpsLimit.Name;
			FPSLimitLocalizer.Apply();
			
			FPSLimitInputSlider.SetValueWithoutNotify(Convert.ToInt32(fpsLimit.Value));

			var renderScale = SettingsManager.Instance.GetSetting("video-renderscale");
			RenderScaleLocalizer.Key = renderScale.Name;
			RenderScaleLocalizer.Apply();
			
			RenderScaleInputSlider.SetValueWithoutNotify(Convert.ToSingle(renderScale.Value));
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
		
		public void OnVSyncChanged(bool value)
		{
			SettingsManager.Instance.SetSetting("video-vsync", value);
		}
		
		public void OnFPSLimitChanged(float value)
		{
			SettingsManager.Instance.SetSetting("video-fpslimit", Convert.ToInt32(value));
		}
		
		public void OnRenderScaleChanged(float value)
		{
			SettingsManager.Instance.SetSetting("video-renderscale", value);
		}
	}
}