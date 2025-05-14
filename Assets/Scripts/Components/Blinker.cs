using Components.Events;
using Cysharp.Threading.Tasks;
using Tools;
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

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnBlinkedEvent, Color.blue);
		}
#endif
		
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