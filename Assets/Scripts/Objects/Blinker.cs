using Cysharp.Threading.Tasks;
using Objects.Base;
using Objects.Events;
using Tools;
using UnityEngine;

namespace Objects
{
	public class Blinker : BaseLight
	{
		[SerializeField]
		public float BlinkEvery;

		[SerializeField]
		public OnBlinkedEvent OnBlinkedEvent;

		#region Identify / SaveLoad

		public override bool ShouldSave => false;

		#endregion
		
		public void Start()
		{
			processBlink().Forget();
		}

#if UNITY_EDITOR
		public override void OnDrawGizmos()
		{
			base.OnDrawGizmos();
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