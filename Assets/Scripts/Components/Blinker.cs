using Components.Events;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Components
{
	public class Blinker : MonoBehaviour
	{
		[SerializeField]
		public float BlinkEvery;

		[SerializeField]
		public OnBlinkedEvent OnBlinkedEvent;

		public void Awake()
		{
			processBlink().Forget();
		}

		private async UniTaskVoid processBlink()
		{
			while (true)
			{
				await UniTask.WaitForSeconds(BlinkEvery);

				if (this == null || !isActiveAndEnabled)
					return;
				
				OnBlinkedEvent?.Invoke();
			}
		}
	}
}