using System;
using System.Collections.Generic;
using Managers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class GraphicsPage : SettingsPage
	{
		[SerializeField]
		public Localizer ShadowQualityLocalizer;
		[SerializeField]
		public DropdownLocalizer ShadowQualityDropdown;
		
		[SerializeField]
		public Localizer TextureQualityLocalizer;
		[SerializeField]
		public DropdownLocalizer TextureQualityDropdown;

		[SerializeField]
		public Localizer ModelQualityLocalizer;
		[SerializeField]
		public DropdownLocalizer ModelQualityDropdown;

		[SerializeField]
		public Localizer ShaderQualityLocalizer;
		[SerializeField]
		public DropdownLocalizer ShaderQualityDropdown;

		[SerializeField]
		public Localizer AntialiasingLocalizer;
		[SerializeField]
		public DropdownLocalizer AntialiasingDropdown;
		
		[SerializeField]
		public Localizer MotionBlurLocalizer;
		[SerializeField]
		public Toggle MotionBlurToggle;
		
		private readonly List<string> maxQualityKeys = new () {"SETTINGS_DROPDOWN_LOW", "SETTINGS_DROPDOWN_MEDIUM", "SETTINGS_DROPDOWN_HIGH", "SETTINGS_DROPDOWN_VERYHIGH"};
		private readonly List<string> qualityKeys = new () {"SETTINGS_DROPDOWN_LOW", "SETTINGS_DROPDOWN_MEDIUM", "SETTINGS_DROPDOWN_HIGH"};
		private readonly List<string> aaKeys = new () {"SETTINGS_DROPDOWN_NONE", "SETTINGS_DROPDOWN_MSAA2X", "SETTINGS_DROPDOWN_MSAA4X", "SETTINGS_DROPDOWN_MSAA8X"};

		public override void Select(bool state)
		{
			base.Select(state);

			var shadowQuality = SettingsManager.Instance.GetSetting("graphics-shadowquality");
			ShadowQualityLocalizer.Key = shadowQuality.Name;
			ShadowQualityLocalizer.Apply();
			
			ShadowQualityDropdown.SetOptions(maxQualityKeys);
			ShadowQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(shadowQuality.Value));
			
			var textureQuality = SettingsManager.Instance.GetSetting("graphics-texturequality");
			TextureQualityLocalizer.Key = textureQuality.Name;
			TextureQualityLocalizer.Apply();
			
			TextureQualityDropdown.SetOptions(qualityKeys);
			TextureQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(textureQuality.Value));
			
			var modelQuality = SettingsManager.Instance.GetSetting("graphics-modelquality");
			ModelQualityLocalizer.Key = modelQuality.Name;
			ModelQualityLocalizer.Apply();
			
			ModelQualityDropdown.SetOptions(maxQualityKeys);
			ModelQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(modelQuality.Value));
			
			var shaderQuality = SettingsManager.Instance.GetSetting("graphics-shaderquality");
			ShaderQualityLocalizer.Key = shaderQuality.Name;
			ShaderQualityLocalizer.Apply();
			
			ShaderQualityDropdown.SetOptions(maxQualityKeys);
			ShaderQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(shaderQuality.Value));
			
			var antiAliasing = SettingsManager.Instance.GetSetting("graphics-antialiasing");
			AntialiasingLocalizer.Key = antiAliasing.Name;
			AntialiasingLocalizer.Apply();
			
			AntialiasingDropdown.SetOptions(aaKeys);
			AntialiasingDropdown.SetValueWithoutNotify(Convert.ToInt32(antiAliasing.Value));
			
			var motionBlur = SettingsManager.Instance.GetSetting("graphics-motionblur");
			MotionBlurLocalizer.Key = motionBlur.Name;
			MotionBlurLocalizer.Apply();
			
			MotionBlurToggle.SetIsOnWithoutNotify(Convert.ToBoolean(motionBlur.Value));
		}

		public override void ResetTab()
		{
			var settings = SettingsManager.Instance.GetSettings();
			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("graphics-"))
					continue;
				
				SettingsManager.Instance.DefaultSetting(pair.Key);
			}
			
			base.ResetTab();
		}
		
		public void OnShadowQualityChanged(int value)
		{
			SettingsManager.Instance.SetSetting("graphics-shadowquality", value);
		}
		
		public void OnTextureQualityChanged(int value)
		{
			SettingsManager.Instance.SetSetting("graphics-texturequality", value);
		}
		
		public void OnModelQualityChanged(int value)
		{
			SettingsManager.Instance.SetSetting("graphics-modelquality", value);
		}
		
		public void OnShaderQualityChanged(int value)
		{
			SettingsManager.Instance.SetSetting("graphics-shaderquality", value);
		}
		
		public void OnAntialiasingChanged(int value)
		{
			SettingsManager.Instance.SetSetting("graphics-antialiasing", value);
		}
		
		public void OnMotionBlurChanged(bool value)
		{
			SettingsManager.Instance.SetSetting("graphics-motionblur", value);
		}
	}
}