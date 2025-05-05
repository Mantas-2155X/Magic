using System;
using System.Text;
using System.Threading;
using AI.Base;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using TMPro;
using UI.Enums;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
	public class Notice : MonoBehaviour
	{
		[SerializeField]
		public Image Image;
		
		[SerializeField]
		public TMP_Text Text;

		private float animationDuration = 0.5f;
		
		private CancellationTokenSource cancellationToken;
		
		public void Awake()
		{
			gameObject.SetActive(false);
			
			BaseAlive.OnDeathEvent.AddListener(OnDeath);
		}

		public void OnDisable()
		{
			ClearMessage();
		}

		public void OnDestroy()
		{
			BaseAlive.OnDeathEvent.RemoveListener(OnDeath);
		}

		public void ShowMessage(ENoticePresetFlags preset)
		{
			var builder = new StringBuilder();
		
			var values = Enum.GetValues(typeof(ENoticePresetFlags));
			for (var i = 0; i < values.Length; i++)
			{
				var value = (ENoticePresetFlags)values.GetValue(i);
				if (value == ENoticePresetFlags.None || !preset.HasFlag(value))
					continue;

				var text = LocalizationManager.Instance.GetLocalizedEntry($"NOTICE_{value.ToString().ToUpper()}");

				switch (value)
				{
					case ENoticePresetFlags.Flashlight:
						text = text.Replace("$0", new InputBinding(SettingsManager.Instance.GetString("keybinds-gameplay-light")).ToDisplayString());
						break;
					case ENoticePresetFlags.Resource:
					case ENoticePresetFlags.Interact:
						text = text.Replace("$0", new InputBinding(SettingsManager.Instance.GetString("keybinds-gameplay-interact")).ToDisplayString());
						break;
					case ENoticePresetFlags.Grab:
						text = text.Replace("$0", new InputBinding(SettingsManager.Instance.GetString("keybinds-gameplay-grab")).ToDisplayString());
						break;
				}
					
				builder.AppendLine(text);
			}
			
			ShowMessage(builder.ToString(), 5f, HorizontalAlignmentOptions.Left, VerticalAlignmentOptions.Middle);
		}
		
		public void ShowMessage(string text, float duration, HorizontalAlignmentOptions horizontalAlignment, VerticalAlignmentOptions verticalAlignment)
		{
			if (cancellationToken != null)
				cancellationToken.Cancel();
			
			cancellationToken = new CancellationTokenSource();
			processMessage(cancellationToken.Token, text, duration, horizontalAlignment, verticalAlignment).Forget();
		}

		public void ClearMessage()
		{
			if (cancellationToken == null)
				return;

			cancellationToken.Cancel();
			gameObject.SetActive(false);
		}

		public void OnDeath(IAlive alive, object source)
		{
			if (alive is not AI.Player)
				return;
			
			ClearMessage();
		}
		
		private async UniTaskVoid processMessage(CancellationToken token, string text, float duration, HorizontalAlignmentOptions horizontalAlignment, VerticalAlignmentOptions verticalAlignment)
		{
			Text.horizontalAlignment = horizontalAlignment;
			Text.verticalAlignment = verticalAlignment;
			
			Text.text = text;
			
			gameObject.SetActive(true);

			await animate(token, true);
			
			if (token.IsCancellationRequested)
			{
				gameObject.SetActive(false);
				return;
			}
			
			await UniTask.WaitForSeconds(duration, ignoreTimeScale: true, cancellationToken: token);
			
			if (token.IsCancellationRequested)
			{
				gameObject.SetActive(false);
				return;
			}
			
			await animate(token, false);

			if (this == null || !isActiveAndEnabled)
				return;
			
			gameObject.SetActive(false);
		}

		private async UniTask animate(CancellationToken token, bool fadeIn)
		{
			var imageColor = Image.color;
			var textColor = Text.color;

			imageColor.a = fadeIn ? 0f : 1f;
			Image.color = imageColor;

			textColor.a = fadeIn ? 0f : 1f;
			Text.color = textColor;

			var normalizedTime = 0f;
			while (normalizedTime < 1f)
			{
				await UniTask.NextFrame(cancellationToken: token);

				if (this == null || !isActiveAndEnabled)
					return;

				if (token.IsCancellationRequested)
				{
					gameObject.SetActive(false);
					return;
				}

				var value = fadeIn ? normalizedTime : 1 - normalizedTime;

				imageColor.a = value;
				Image.color = imageColor;
				
				textColor.a = value;
				Text.color = textColor;
				
				normalizedTime += Time.unscaledDeltaTime / animationDuration;
			}
			
			imageColor.a = fadeIn ? 1f : 0f;
			Image.color = imageColor;

			textColor.a = fadeIn ? 1f : 0f;
			Text.color = textColor;
		}
	}
}