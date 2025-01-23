using System.Collections.Generic;
using UnityEngine;

namespace UI.Settings
{
	public class Settings : MonoBehaviour
	{
		[SerializeField]
		public List<SettingsPage> Pages;
		
		public SettingsPage CurrentPage { get; private set; }

		public void Awake()
		{
			if (Pages.Count == 0)
				return;

			for (var i = 0; i < Pages.Count; i++)
			{
				var page = Pages[i];
				page.Tab.onClick.AddListener(delegate
				{
					SelectPage(page);
				});
			}

			SelectPage(Pages[0]);
		}

		public void SelectPage(SettingsPage page)
		{
			if (CurrentPage == page)
				return;
			
			if (CurrentPage != null)
				CurrentPage.Select(false);
			
			CurrentPage = page;
			CurrentPage.Select(true);
		}
		
		public void OnEnable()
		{
			transform.SetAsLastSibling();
		}

		public void OnCloseClicked()
		{
			Display(false);
		}

		public void Toggle()
		{
			Display(!isActiveAndEnabled);
		}
		
		public void Display(bool state)
		{
			if (state == isActiveAndEnabled)
				return;
			
			if (state)
			{
				var title = Title.Instance;
				if (title != null && !title.isActiveAndEnabled)
					title.Open();
			}

			gameObject.SetActive(state);
		}
	}
}