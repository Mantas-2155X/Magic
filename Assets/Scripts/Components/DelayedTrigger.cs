using System.Collections.Generic;
using Events;
using Managers;
using Newtonsoft.Json.Linq;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;

namespace Components
{
	public class DelayedTrigger : MonoBehaviour, ISaveable
	{
		[field: SerializeField]
		public string ObjectID { get; set; }

		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;
	
		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public float TriggerAfter;

		public bool Triggered { get; private set; }

		private float enterTime = -1f;
		private Collider enterCollider;

		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var triggerState = TriggerState.Read(this);
			if (triggerState != null)
				dict[typeof(DelayedTrigger).ToString()] = JObject.FromObject(triggerState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(DelayedTrigger).ToString(), out var triggerState))
				TriggerState.Apply(this, triggerState.ToObject<TriggerState>());
		}
		
		public void SetState(bool triggered)
		{
			Triggered = triggered;
		}
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Triggered || enterTime < 0f)
				return;

			if (Time.time < enterTime + TriggerAfter)
				return;
		
			if (!IsMultiTrigger)
				Triggered = true;
			
			enterTime = -1f;
			enterCollider = null;
			
			OnTriggerEvent.Invoke(enterCollider);
		}
	
		public void OnTriggerEnter(Collider other)
		{
			if (Triggered)
				return;
		
			enterTime = Time.time;
			enterCollider = other;
		}
	
		public void OnTriggerExit(Collider other)
		{
			if (Triggered)
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