using System;
using System.Collections.Generic;
using Managers;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings.Pages
{
	public class AudioPage : SettingsPage
	{
		[SerializeField]
		public Localizer MasterVolumeLocalizer;
		[SerializeField]
		public InputSlider MasterVolumeInputSlider;
		
		[SerializeField]
		public Localizer SFXVolumeLocalizer;
		[SerializeField]
		public InputSlider SFXVolumeInputSlider;

		public override void Select(bool state)
		{
			base.Select(state);
			
			var masterVolume = SettingsManager.Instance.GetSetting("audio-mastervolume");
			MasterVolumeLocalizer.Key = masterVolume.Name;
			MasterVolumeLocalizer.Apply();
			
			MasterVolumeInputSlider.SetValueWithoutNotify(Convert.ToSingle(masterVolume.Value));
			
			var sfxVolume = SettingsManager.Instance.GetSetting("audio-sfxvolume");
			SFXVolumeLocalizer.Key = sfxVolume.Name;
			SFXVolumeLocalizer.Apply();
			
			SFXVolumeInputSlider.SetValueWithoutNotify(Convert.ToSingle(sfxVolume.Value));
		}

		public override void ResetTab()
		{
			var settings = SettingsManager.Instance.GetSettings();
			foreach (var pair in settings)
			{
				if (!pair.Key.StartsWith("audio-"))
					continue;
				
				SettingsManager.Instance.DefaultSetting(pair.Key);
			}
			
			base.ResetTab();
		}

		public void OnMasterVolumeChanged(float value)
		{
			SettingsManager.Instance.SetSetting("audio-mastervolume", value);
		}
		
		public void OnSFXVolumeChanged(float value)
		{
			SettingsManager.Instance.SetSetting("audio-sfxvolume", value);
		}
	}
}