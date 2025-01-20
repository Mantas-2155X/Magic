using Managers;
using TMPro;
using UnityEngine;

namespace UI
{
	public class Localizer : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text Text;

		[SerializeField]
		public string Key;
		
		public void Awake()
		{
			LocalizationManager.Instance.RegisterLocalizer(this);
			UpdateText();
		}

		public void OnDestroy()
		{
			LocalizationManager.Instance.UnregisterLocalizer(this);
		}

		public void UpdateText()
		{
			Text.text = LocalizationManager.Instance.GetLocalizedEntry(Key);
		}
	}
}