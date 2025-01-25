using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
	public class SettingsPage : MonoBehaviour
	{
		[SerializeField]
		public Button Tab;

		[SerializeField]
		public Localizer TabLocalizer;

		[SerializeField]
		public GameObject AutoSelect;
		
		public virtual void Select(bool state)
		{
			TabLocalizer.Text.fontStyle = state ? FontStyles.Italic : FontStyles.Normal;
			gameObject.SetActive(state);
		}
	}
}