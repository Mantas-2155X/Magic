using System;
using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;

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

		private readonly List<string> qualityKeys = new () {"SETTINGS_DROPDOWN_LOW", "SETTINGS_DROPDOWN_MEDIUM", "SETTINGS_DROPDOWN_HIGH"};

		public override void Select(bool state)
		{
			base.Select(state);

			var shadowQuality = SettingsManager.Instance.GetSetting("graphics-shadowquality");
			ShadowQualityLocalizer.Key = shadowQuality.Name;
			ShadowQualityLocalizer.Apply();
			
			ShadowQualityDropdown.SetOptions(qualityKeys);
			ShadowQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(shadowQuality.Value));
			
			var textureQuality = SettingsManager.Instance.GetSetting("graphics-texturequality");
			TextureQualityLocalizer.Key = textureQuality.Name;
			TextureQualityLocalizer.Apply();
			
			TextureQualityDropdown.SetOptions(qualityKeys);
			TextureQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(textureQuality.Value));
			
			var modelQuality = SettingsManager.Instance.GetSetting("graphics-modelquality");
			ModelQualityLocalizer.Key = modelQuality.Name;
			ModelQualityLocalizer.Apply();
			
			ModelQualityDropdown.SetOptions(qualityKeys);
			ModelQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(modelQuality.Value));
			
			var shaderQuality = SettingsManager.Instance.GetSetting("graphics-shaderquality");
			ShaderQualityLocalizer.Key = shaderQuality.Name;
			ShaderQualityLocalizer.Apply();
			
			ShaderQualityDropdown.SetOptions(qualityKeys);
			ShaderQualityDropdown.SetValueWithoutNotify(Convert.ToInt32(shaderQuality.Value));
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
	}
}