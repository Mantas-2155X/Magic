using Events;
using Managers;
using Tools;
using UnityEngine;

namespace Components
{
	public class DelayedTrigger : MonoBehaviour
	{
		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;
	
		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public float TriggerAfter;

		private bool triggered;

		private float enterTime = -1f;
		private Collider enterCollider;

		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (triggered || enterTime < 0f)
				return;

			if (Time.time < enterTime + TriggerAfter)
				return;
		
			if (!IsMultiTrigger)
				triggered = true;
			
			enterTime = -1f;
			enterCollider = null;
			
			OnTriggerEvent.Invoke(enterCollider);
		}
	
		public void OnTriggerEnter(Collider other)
		{
			if (triggered)
				return;
		
			enterTime = Time.time;
			enterCollider = other;
		}
	
		public void OnTriggerExit(Collider other)
		{
			if (triggered)
				return;

			enterTime = -1f;
			enterCollider = null;
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnTriggerEvent, Color.blue);
		}
#endif
	}
}