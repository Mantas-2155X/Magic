using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Settings
{
	public class Settings : MonoBehaviour
	{
		[SerializeField]
		public List<SettingsPage> Pages;
		
		[SerializeField]
		public Button CloseButton;

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
					SelectionManager.Instance.SetSelection(page.AutoSelect);
				});

				var nav = new Navigation
				{
					mode = Navigation.Mode.Explicit
				};

				if (i == 0)
				{
					nav.selectOnLeft = Pages[^1].Tab;
					nav.selectOnRight = Pages.Count == 1 ? page.Tab : Pages[i + 1].Tab;
				}
				else if (i == Pages.Count - 1)
				{
					nav.selectOnLeft = Pages.Count == 1 ? page.Tab : Pages[i - 1].Tab;
					nav.selectOnRight = Pages[0].Tab;
				}
				else
				{
					nav.selectOnLeft = Pages[i - 1].Tab;
					nav.selectOnRight = Pages[i + 1].Tab;
				}

				nav.selectOnUp = CloseButton;
				
				page.Tab.navigation = nav;
			}

			CloseButton.navigation = new Navigation
			{
				mode = Navigation.Mode.Explicit,
				selectOnUp = Pages[0].Tab,
				selectOnDown = Pages[^1].Tab
			};
			
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
			Select();
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
			
			if (state)
				Select();
			else
				Title.Instance.Select(true);
		}
		
		public void Select()
		{
			selectDelayed().Forget();
		}
		
		private async UniTaskVoid selectDelayed()
		{
			await UniTask.NextFrame();
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			SelectionManager.Instance.SetSelection(Pages[0].Tab.gameObject);
		}
	}
}