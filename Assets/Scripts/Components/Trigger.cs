using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
		public bool ShouldSave => true;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

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
	}
}