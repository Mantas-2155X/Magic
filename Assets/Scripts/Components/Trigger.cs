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
	public class Trigger : MonoBehaviour, ISaveable
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
		public bool TriggerOnStay;
	
		public bool Triggered { get; private set; }
	
		#region Identify / SaveLoad
		
		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var triggerState = TriggerState.Read(this);
			if (triggerState != null)
				dict[typeof(Trigger).ToString()] = JObject.FromObject(triggerState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Trigger).ToString(), out var triggerState))
				TriggerState.Apply(this, triggerState.ToObject<TriggerState>());
		}

		public void SetState(bool triggered)
		{
			Triggered = triggered;
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

		public void OnTriggerEnter(Collider other)
		{
			if (Triggered)
				return;

			if (!IsMultiTrigger)
				Triggered = true;
		
			OnTriggerEvent?.Invoke(other);
		}
	
		public void OnTriggerStay(Collider other)
		{
			if (Triggered || !TriggerOnStay)
				return;

			if (!IsMultiTrigger)
				Triggered = true;
		
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