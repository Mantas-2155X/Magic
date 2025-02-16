using Events;
using Tools;
using UnityEngine;

namespace Components
{
	public class Trigger : MonoBehaviour
	{
		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;

		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public bool TriggerOnStay;
	
		private bool triggered;
	
		public void OnTriggerEnter(Collider other)
		{
			if (triggered)
				return;

			if (!IsMultiTrigger)
				triggered = true;
		
			OnTriggerEvent?.Invoke(other);
		}
	
		public void OnTriggerStay(Collider other)
		{
			if (triggered || !TriggerOnStay)
				return;

			if (!IsMultiTrigger)
				triggered = true;
		
			OnTriggerEvent?.Invoke(other);
		}
	
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnTriggerEvent, Color.blue);
		}
#endif
	}
}