using System.Collections.Generic;
using Events;
using Managers;
using Newtonsoft.Json.Linq;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Components
{
	public class DelayedTrigger : MonoBehaviour, ISaveable
	{
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set
			{
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects.Remove(objectID);
				objectID = value;
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects[objectID] = this;
			}
		}

		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;
	
		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public float TriggerAfter;

		public bool Triggered { get; private set; }
		public float EnterTime { get; private set; } = -1f;

		public Collider EnterCollider { get; private set; }

		private float? adjustNextEnter;

		#region Identify / SaveLoad
		
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
		
		public void SetState(bool triggered, float? enterTime)
		{
			Triggered = triggered;
			adjustNextEnter = enterTime;
		}
		
		public void Awake()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects[ObjectID] = this;
		}

		public void OnDestroy()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects.Remove(ObjectID);
		}
		
		#endregion
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Triggered || EnterCollider == null)
				return;

			if (Time.time < EnterTime + TriggerAfter)
				return;
		
			if (!IsMultiTrigger)
				Triggered = true;
			
			EnterTime = 0f;
			EnterCollider = null;
			
			OnTriggerEvent.Invoke(EnterCollider);
		}
	
		public void OnTriggerEnter(Collider other)
		{
			if (Triggered)
				return;
		
			EnterTime = Time.time;
			EnterCollider = other;
			
			if (adjustNextEnter != null)
				EnterTime -= adjustNextEnter.Value;
		}
	
		public void OnTriggerExit(Collider other)
		{
			if (Triggered)
				return;

			EnterTime = 0f;
			EnterCollider = null;
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnTriggerEvent, Color.blue);
		}
#endif
	}
}