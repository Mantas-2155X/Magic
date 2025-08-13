using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Managers;
using Tools;
using UI.Enums;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
	public class Confirm : MonoBehaviour
	{
		[SerializeField]
		public Localizer Text;

		[SerializeField]
		public Image Fade;

		[SerializeField]
		public Button YesButton;
		
		public float FadeDuration = 0.175f;
		
		private CancellationTokenSource cancellationToken = new ();

		private EConfirmPreset currentPreset;
		private Action<bool> currentCallback;
		
		private GameObject previousSelection;
		private float normalizedTime;
		
		public void Show(EConfirmPreset preset, Action<bool> callback)
		{
			previousSelection = SelectionManager.Instance.Selection;
			
			currentPreset = preset;
			currentCallback = callback;
			
			Text.Key = LocalizationManager.Instance.GetLocalizedEntry($"CONFIRM_{preset.ToString().ToUpper()}");
			Text.Apply();
			
			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();
			
			showAsync(cancellationToken.Token).Forget();
		}

		public void OnYesClicked()
		{
			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();
			
			hideAsync(cancellationToken.Token).Forget();
			currentCallback.Invoke(true);
		}

		public void OnNoClicked()
		{
			cancellationToken?.Cancel();
			cancellationToken = new CancellationTokenSource();

			hideAsync(cancellationToken.Token).Forget();
			currentCallback.Invoke(false);
		}

		private async UniTaskVoid showAsync(CancellationToken token)
		{
			gameObject.SetActive(true);
			SelectionManager.Instance.SetSelection(YesButton.gameObject);
			
			await fadeAsync(token, true);
		}
		
		private async UniTaskVoid hideAsync(CancellationToken token)
		{
			await fadeAsync(token, false);
			
			if (this == null || !isActiveAndEnabled || token.IsCancellationRequested)
				return;
			
			gameObject.SetActive(false);
			
			if (previousSelection != null && previousSelection.activeInHierarchy)
				SelectionManager.Instance.SetSelection(previousSelection);
		}

		private async UniTask fadeAsync(CancellationToken token, bool fadeIn)
		{
			var color = Fade.color;

			while (fadeIn ? normalizedTime < 1f : normalizedTime > 0f)
			{
				await UniTask.NextFrame(cancellationToken: token);

				if (this == null || !isActiveAndEnabled || token.IsCancellationRequested)
					return;

				color.a = MathTools.Remap(normalizedTime, 0f, 1f, 0f, 0.4f);
				Fade.color = color;
				
				if (fadeIn)
					normalizedTime += Time.unscaledDeltaTime / FadeDuration;
				else
					normalizedTime -= Time.unscaledDeltaTime / FadeDuration;
			}
			
			color.a = MathTools.Remap(fadeIn ? 1f : 0f, 0f, 1f, 0f, 0.4f);
			Fade.color = color;
		}
	}
}