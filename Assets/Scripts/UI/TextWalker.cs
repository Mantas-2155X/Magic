using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UI.Enums;
using UnityEngine;
using UnityEngine.Events;

namespace UI
{
	public class TextWalker : MonoBehaviour
	{
		[SerializeField]
		public TMP_Text Text;
		
		public ETextWalkerState CurrentState { get; private set; }
		public string CurrentText { get; private set; }
		public int CurrentCharacter { get; private set; }
		
		private CancellationTokenSource cancellationToken = new ();
		
		public void Walk(string text, float startDelay, float endDelay, float startCharacterDelay, float endCharacterDelay, UnityAction onFinishedCallback = null)
		{
			if (cancellationToken != null)
				cancellationToken?.Cancel();
			
			CurrentText = text;
			CurrentCharacter = 0;
			
			cancellationToken = new CancellationTokenSource();
			textLoop(startDelay, endDelay, startCharacterDelay, endCharacterDelay, onFinishedCallback, cancellationToken.Token).Forget();
		}

		private async UniTaskVoid textLoop(float startDelay, float endDelay, float startCharacterDelay, float endCharacterDelay, UnityAction onFinishedCallback, CancellationToken token)
		{
			CurrentState = ETextWalkerState.StartWait;
			await UniTask.WaitForSeconds(startDelay, cancellationToken: token);
			
			if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
				return;

			CurrentState = ETextWalkerState.Starting;
			await startText(startCharacterDelay, token);

			if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
				return;

			CurrentState = ETextWalkerState.EndWait;
			await UniTask.WaitForSeconds(endDelay, cancellationToken: token);
			
			if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
				return;

			CurrentState = ETextWalkerState.Ending;
			await endText(endCharacterDelay, token);

			CurrentState = ETextWalkerState.Done;
			onFinishedCallback?.Invoke();
		}
		
		private async UniTask startText(float delayBetweenCharacters, CancellationToken token)
		{
			while (CurrentCharacter < CurrentText.Length)
			{
				await UniTask.WaitForSeconds(delayBetweenCharacters, cancellationToken: token);
				
				if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
					return;
				
				Text.text = CurrentText[..(CurrentCharacter + 1)];
				CurrentCharacter++;
			}
		}
		
		private async UniTask endText(float delayBetweenCharacters, CancellationToken token)
		{
			while (CurrentCharacter >= 0)
			{
				await UniTask.WaitForSeconds(delayBetweenCharacters, cancellationToken: token);
				
				if (token.IsCancellationRequested || this == null || !isActiveAndEnabled)
					return;
				
				Text.text = CurrentText[..CurrentCharacter];
				CurrentCharacter--;
			}
		}
	}
}