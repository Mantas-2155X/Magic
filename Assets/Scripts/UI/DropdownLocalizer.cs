using System.Collections.Generic;
using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
	public class DropdownLocalizer : Localizer
	{
		[SerializeField]
		public TMP_Dropdown Dropdown;

		private readonly List<string> rawOptions = new ();
		private readonly List<string> localizedOptions = new ();

		#region MonoBehaviour

		public override void Awake()
		{
			base.Awake();
			Dropdown.onValueChanged.AddListener(onValueChanged);
		}
		
		public override void OnDestroy()
		{
			base.OnDestroy();
			Dropdown.onValueChanged.RemoveListener(onValueChanged);
		}
		
		#endregion

		#region Localizer

		public override void Apply()
		{
			localizeOptions();
			localizeCaption();
		}
		
		#endregion

		#region Dropdown

		public void SetOptions(List<string> options)
		{
			ClearOptions();

			var localizationManager = LocalizationManager.Instance;
			
			for (var i = 0; i < options.Count; i++)
			{
				var option = options[i];
				
				rawOptions.Add(option);
				localizedOptions.Add(localizationManager.GetLocalizedEntry(option));
			}
			
			Dropdown.AddOptions(localizedOptions);
		}

		public void ClearOptions()
		{
			rawOptions.Clear();
			localizedOptions.Clear();
			
			Dropdown.ClearOptions();
		}

		public void SetValueWithoutNotify(int value)
		{
			Dropdown.SetValueWithoutNotify(value);
			localizeCaption();
		}
		
		#endregion

		#region Internal

		private void localizeOptions()
		{
			localizedOptions.Clear();
			
			var options = new List<TMP_Dropdown.OptionData>();
			var localizationManager = LocalizationManager.Instance;

			for (var i = 0; i < rawOptions.Count; i++)
			{
				var localized = localizationManager.GetLocalizedEntry(rawOptions[i]);
				
				localizedOptions.Add(localized);
				options.Add(new TMP_Dropdown.OptionData(localized));
			}

			Dropdown.options = options;
			localizeCaption();
		}

		private void localizeCaption()
		{
			var value = Dropdown.value;
			if (value == -1 || value >= localizedOptions.Count)
			{
				Dropdown.captionText.text = "";
				return;
			}
			
			Dropdown.captionText.text = localizedOptions[value];
		}
		
		private void onValueChanged(int value)
		{
			localizeCaption();
		}
		
		#endregion
	}
}