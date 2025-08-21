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
	public class DelayedTrigger : MonoBehaviour, ISaveable
	{
		[SerializeField]
		public OnTriggerEvent OnTriggerEvent;
	
		[SerializeField]
		public bool IsMultiTrigger;

		[SerializeField]
		public float TriggerAfter;

		public bool Triggered { get; private set; }
		public float EnterTime { get; private set; } = -1f;
		public IIdentifiable EnterObject { get; private set; }

		private float? adjustNextEnter;

		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;
		
		#region Identify / SaveLoad
		
		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => false;
		
		public virtual bool ExternallySpawned { get; set; } = false;

		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
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
			dict[typeof(DelayedTrigger).ToString()] = JObject.FromObject(new DelayedTriggerState(this));
			
			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(DelayedTrigger).ToString(), out var delayedTriggerState) && delayedTriggerState != null)
				delayedTriggerState.ToObject<DelayedTriggerState>().Apply(this);
		}
		
		public void SetState(bool triggered, float? enterTime, string enterObjectID)
		{
			Triggered = triggered;
			adjustNextEnter = enterTime;
			EnterObject = StateManager.Instance.GetRegisteredObject(enterObjectID);
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
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (Triggered || EnterObject.IsNull())
				return;

			if (Time.time < EnterTime + TriggerAfter)
				return;
		
			if (!IsMultiTrigger)
				Triggered = true;
			
			EnterTime = 0f;
			EnterObject = null;
			
			OnTriggerEvent.Invoke(EnterObject);
		}
	
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
			
			EnterTime = Time.time;
			EnterObject = identifiable;
			
			if (adjustNextEnter != null)
				EnterTime -= adjustNextEnter.Value;
		}
	
		public void OnTriggerExit(Collider other)
		{
			if (Triggered)
				return;

			EnterTime = 0f;
			EnterObject = null;
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
		public class DelayedTriggerState : IState
		{
			[JsonProperty]
			public bool Triggered;
				
			[JsonProperty]
			public float? EnterTime;

			[JsonProperty]
			public string EnterObjectID;

			public DelayedTriggerState() { }
			
			public DelayedTriggerState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not DelayedTrigger delayedTrigger)
					return;

				Triggered = delayedTrigger.Triggered;

				if (delayedTrigger.EnterObject.NotNull())
				{
					EnterTime = Time.time - delayedTrigger.EnterTime;
					EnterObjectID = delayedTrigger.EnterObject.ObjectID;
				}
			}
			
			public void Apply(object obj)
			{
				if (obj is not DelayedTrigger delayedTrigger)
					return;

				delayedTrigger.SetState(Triggered, EnterTime, EnterObjectID);
			}
		}
	}
}