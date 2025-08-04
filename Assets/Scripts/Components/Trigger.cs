using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Events;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using State.Enums;
using State.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Components
{
	public class Trigger : MonoBehaviour, ISaveable
	{
		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;

		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public bool TriggerOnStay;
	
		public bool Triggered { get; private set; }
	
		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad
		
		public virtual bool ShouldSave => true;
		
		public virtual bool ExternallySpawned { get; set; } = false;

		public virtual ELoadType LoadType => ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Late;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			throw new NotImplementedException();
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(Trigger).ToString()] = JObject.FromObject(new TriggerState(this));
			
			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Trigger).ToString(), out var triggerState) && triggerState != null)
				triggerState.ToObject<TriggerState>().Apply(this);
		}

		public void SetState(bool triggered)
		{
			Triggered = triggered;
		}
		
		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion

		public void OnTriggerEnter(Collider other)
		{
			if (Triggered)
				return;

			var rb = other.attachedRigidbody;
			if (rb == null)
				return;
			
			var identifiable = rb.GetComponent<IIdentifiable>();
			if (identifiable.IsNull())
				return;

			if (!IsMultiTrigger)
				Triggered = true;
		
			OnTriggerEvent?.Invoke(identifiable);
		}
	
		public void OnTriggerStay(Collider other)
		{
			if (Triggered || !TriggerOnStay)
				return;

			var rb = other.attachedRigidbody;
			if (rb == null)
				return;

			var identifiable = rb.GetComponent<IIdentifiable>();
			if (identifiable.IsNull())
				return;
			
			if (!IsMultiTrigger)
				Triggered = true;
		
			OnTriggerEvent?.Invoke(identifiable);
		}
	
#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			EventTools.DrawListeners(transform, OnTriggerEvent, Color.blue);
		}
#endif
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}
		
		[JsonObject]
		public class TriggerState : IState
		{
			[JsonProperty]
			public bool Triggered;

			[JsonProperty]
			public string EnterObjectID;

			public TriggerState() { }
			
			public TriggerState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not Trigger trigger)
					return;

				Triggered = trigger.Triggered;
			}
			
			public void Apply(object obj)
			{
				if (obj is not Trigger trigger)
					return;

				trigger.SetState(Triggered);
			}
		}
	}
}