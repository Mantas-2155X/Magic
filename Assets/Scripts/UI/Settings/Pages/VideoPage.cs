using System;
using System.Collections.Generic;
using Managers;
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
		public DropdownLocalizer ResolutionDropdown;

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
			
			ResolutionDropdown.ClearOptions();
			ResolutionDropdown.SetOptions(resolutions);

			var currentResolution = resolution.Value.ToString();
			var options = ResolutionDropdown.Dropdown.options;

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

			updateFPSLimitSlider();
		}

		public override void ResetTab()
		{
			var settings = SettingsManager.Instance.GetSettings();
			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("video-"))
					continue;
				
				SettingsManager.Instance.DefaultSetting(pair.Key);
			}
			
			base.ResetTab();
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
			updateFPSLimitSlider();
		}
		
		public void OnFPSLimitChanged(float value)
		{
			SettingsManager.Instance.SetSetting("video-fpslimit", Convert.ToInt32(value));
		}
		
		public void OnRenderScaleChanged(float value)
		{
			SettingsManager.Instance.SetSetting("video-renderscale", value);
		}

		private void updateFPSLimitSlider()
		{
			// FPS limit doesn't work with VSync

			var sliderBackground = FPSLimitInputSlider.Slider.transform.Find("Background").GetComponent<Image>();
			var sliderHandle = FPSLimitInputSlider.Slider.handleRect.GetComponent<Image>();

			var inputFieldImage = FPSLimitInputSlider.InputField.GetComponent<Image>();
			
			if (SettingsManager.Instance.GetBool("video-vsync") == true)
			{
				FPSLimitInputSlider.Slider.interactable = false;
				FPSLimitInputSlider.InputField.interactable = false;
				
				sliderBackground.color = Color.gray;
				sliderHandle.color = Color.gray;
				
				inputFieldImage.color = Color.gray;
			}
			else
			{
				FPSLimitInputSlider.Slider.interactable = true;
				FPSLimitInputSlider.InputField.interactable = true;
				
				sliderBackground.color = Color.black;
				sliderHandle.color = Color.black;
				
				inputFieldImage.color = Color.black;
			}
		}
	}
}