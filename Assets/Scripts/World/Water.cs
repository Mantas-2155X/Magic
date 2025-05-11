using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace World
{
	public class Water : MonoBehaviour, ISaveable
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
		public float DamageRate = 0.1f;

		[SerializeField]
		public float Damage = 12;

		public List<IAlive> Alives { get; private set; } = new ();
		
		private GameObject thisGo;
		private Transform thisTr;
		
		private bool init;

		#region Identify / SaveLoad
		
		public Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();
			
			var waterState = WaterState.Read(this);
			if (waterState != null)
				dict[typeof(Water).ToString()] = JObject.FromObject(waterState);
			
			return dict;
		}

		public void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Water).ToString(), out var waterState))
				WaterState.Apply(this, waterState.ToObject<WaterState>());
		}

		public void SetState(List<string> alivesObjectIDs)
		{
			Alives.Clear();

			for (var i = 0; i < alivesObjectIDs.Count; i++)
			{
				var identifiable = StateManager.Instance.GetRegisteredObject(alivesObjectIDs[i]);
				if (identifiable.IsNull() || identifiable is not IAlive alive)
					continue;

				Alives.Add(alive);
			}
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
		
		public void OnEnable()
		{
			damage().Forget();
		}

		public void OnTriggerEnter(Collider other)
		{
			var rb = other.attachedRigidbody;
			if (rb == null)
				return;
			
			var identifiable = rb.GetComponent<IIdentifiable>();
			if (identifiable.IsNull() || identifiable is not IAlive alive || Alives.Contains(alive))
				return;
			
			Alives.Add(alive);
		}

		public void OnTriggerExit(Collider other)
		{
			var rb = other.attachedRigidbody;
			if (rb == null)
				return;

			var identifiable = rb.GetComponent<IIdentifiable>();
			if (identifiable.IsNull() || identifiable is not IAlive alive)
				return;
			
			Alives.Remove(alive);
		}

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
		
		private async UniTaskVoid damage()
		{
			while (true)
			{
				await UniTask.WaitForSeconds(DamageRate);
			
				if (this == null || !isActiveAndEnabled)
					return;
				
				foreach (var alive in Alives)
				{
					if (alive.IsNull() || !alive.IsAlive)
						continue;
				
					alive.Damage(Damage, this, EElement.Unknown);
				}
			}
		}
	}
}