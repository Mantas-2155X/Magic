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

		[SerializeField]
		public Localizer UIVolumeLocalizer;
		[SerializeField]
		public InputSlider UIVolumeInputSlider;

		[SerializeField]
		public Localizer PerspectiveCorrectionLocalizer;
		[SerializeField]
		public InputSlider PerspectiveCorrectionInputSlider;

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
			
			var uiVolume = SettingsManager.Instance.GetSetting("audio-uivolume");
			UIVolumeLocalizer.Key = uiVolume.Name;
			UIVolumeLocalizer.Apply();
			
			UIVolumeInputSlider.SetValueWithoutNotify(Convert.ToSingle(uiVolume.Value));
			
			var perspectiveCorrection = SettingsManager.Instance.GetSetting("audio-perspectivecorrection");
			PerspectiveCorrectionLocalizer.Key = perspectiveCorrection.Name;
			PerspectiveCorrectionLocalizer.Apply();
			
			PerspectiveCorrectionInputSlider.SetValueWithoutNotify(Convert.ToSingle(perspectiveCorrection.Value));

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
		
		public void OnUIVolumeChanged(float value)
		{
			SettingsManager.Instance.SetSetting("audio-uivolume", value);
		}
		
		public void OnPerspectiveCorrectionChanged(float value)
		{
			SettingsManager.Instance.SetSetting("audio-perspectivecorrection", value);
		}
	}
}