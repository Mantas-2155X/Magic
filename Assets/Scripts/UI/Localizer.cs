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
		
		public virtual void Awake()
		{
			LocalizationManager.Instance.RegisterLocalizer(this);
		}

		public virtual void OnDestroy()
		{
			LocalizationManager.Instance.UnregisterLocalizer(this);
		}

		public virtual void Apply()
		{
			if (Text == null)
				return;
			
			Text.text = LocalizationManager.Instance.GetLocalizedEntry(Key);
		}
	}
}