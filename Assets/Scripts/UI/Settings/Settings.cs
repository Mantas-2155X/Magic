using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Managers;
using UI.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Settings
{
	public class Settings : MonoBehaviour
	{
		[SerializeField]
		public List<SettingsPage> Pages;
		
		[SerializeField]
		public Button CloseButton;

		[SerializeField]
		public Button ResetTabButton;

		[SerializeField]
		public Button ResetEverythingButton;

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

			var buttons = new [] { CloseButton, ResetTabButton, ResetEverythingButton };
			
			for (var i = 0; i < buttons.Length; i++)
			{
				var button = buttons[i];
				
				var navigation = button.navigation;
				navigation.selectOnUp = Pages[0].Tab;
				navigation.selectOnDown = Pages[^1].Tab;

				button.navigation = navigation;
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
			Select();
		}

		public void OnCloseClicked()
		{
			Display(false);
		}

		public void OnResetTabClicked()
		{
			Title.Instance.Confirm.Show(EConfirmPreset.ResetTabSettings, result =>
			{
				if (!result)
					return;
				
				if (CurrentPage == null)
					return;
			
				UnityEngine.Debug.Log($"[Settings] Resetting {CurrentPage.TabLocalizer.Text.text} settings");
			
				CurrentPage.ResetTab();
				CurrentPage.Select(true);
			});
		}
		
		public void OnResetEverythingClicked()
		{
			Title.Instance.Confirm.Show(EConfirmPreset.ResetAllSettings, result =>
			{
				if (!result)
					return;
				
				UnityEngine.Debug.Log("[Settings] Resetting all settings");
				SettingsManager.Instance.ResetSettings();
			
				if (CurrentPage == null)
					return;

				CurrentPage.Select(true);
			});
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
				if (!title.isActiveAndEnabled)
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