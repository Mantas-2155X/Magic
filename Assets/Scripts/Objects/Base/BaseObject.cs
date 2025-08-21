//#define DEBUG_OBJ

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Enums;
using Components;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Enums;
using Objects.Interfaces;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using State.States;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

namespace Objects.Base
{
	public class BaseObject : MonoBehaviour, IObject
	{
		[field: SerializeField]
		public ObjectData ObjectData { get; set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }

		public Vector3? LastHitPoint { get; private set; }
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		#region Identify / SaveLoad

		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => true;
		
		public virtual bool ExternallySpawned { get; set; }

		public virtual bool Transferred { get; set; }
		
		public virtual ELoadType LoadType => ExternallySpawned ? ELoadType.Create : ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Normal;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<CreateData>();
			
			var obj = (BaseObject)ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetData<ObjectData>(createData.Name), Vector3.zero, Vector3.zero);
			obj.ObjectID = data.Item1;

			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[BaseObject] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		public virtual JObject GetCreation()
		{
			var createData = new CreateData
			{
				Name = ObjectData.Name,
				States = GetModifications()
			};

			return JObject.FromObject(createData);
		}

		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(Transform).ToString()] = JObject.FromObject(new TransformState(thisTr));
			
			if (Rigidbody != null)
				dict[typeof(Rigidbody).ToString()] = JObject.FromObject(new RigidbodyState(Rigidbody));
			
			dict[typeof(BaseObject).ToString()] = JObject.FromObject(new BaseObjectState(this));

			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState) && transformState != null)
				transformState.ToObject<TransformState>().Apply(thisTr);
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState) && rigidbodyState != null)
				rigidbodyState.ToObject<RigidbodyState>().Apply(Rigidbody);
			
			if (data.TryGetValue(typeof(BaseObject).ToString(), out var baseObjectState) && baseObjectState != null)
				baseObjectState.ToObject<BaseObjectState>().Apply(this);
		}

		public virtual void Awake()
		{
			StateManager.Instance.RegisterObject(this);
			initializeObject();
		}
		
		public virtual void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		#region MonoBehaviour

		public virtual void OnEnable()
		{
			initializeObject();
			
			if (ObjectData.IsBreakable)
				initializeBreakable();
			
			if (ObjectData.IsPickupable)
				initializePickupable();
			
			if (ObjectData.IsUsable)
				initializeUsable();
		}

		public virtual void OnDisable()
		{
			if (ObjectData.IsPoolable != EObjectPool.OnDisable)
				return;

			ObjectID = "";
			PoolingManager.Instance.Add(ObjectData, thisGo);
		}
		
		public virtual void OnParticleSystemStopped()
		{
			if (ObjectData.IsPoolable != EObjectPool.OnParticleSystemStopped)
				return;
			
			ObjectID = "";
			PoolingManager.Instance.Add(ObjectData, thisGo);
		}

		#endregion

		#region Object

		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}
		
		public virtual void Spawn(Vector3 position, Vector3 angles)
		{
			initializeObject();
			
			thisTr.SetParent(World.World.Instance.Objects);
			thisTr.position = position;
			thisTr.eulerAngles = angles;
			
			thisGo.SetActive(true);
		}
		
		#endregion
		
		#region Breakable

		public float Health { get; set; }

		private void initializeBreakable()
		{
			Health = ObjectData.MaximumHealth;
		}
		
		public virtual void Damage(float damage, object source, EElement type, Vector3? hitPoint = null)
		{
			LastHitPoint = hitPoint;
			
			if (!ObjectData.IsBreakable || damage < 0)
				return;
			
			Health -= damage;

			if (Health > 0)
				return;
			
			Break(source);
		}
		
		public virtual void Break(object source)
		{
			if (!ObjectData.IsBreakable)
				return;

			if (ObjectData.BrokenPrefabReference != null && SettingsManager.Instance.GetInt("graphics-modelquality") >= 2)
			{
				var brokenPrefab = Addressables.InstantiateAsync(ObjectData.BrokenPrefabReference, World.World.Instance.Ragdolls).WaitForCompletion();
				
				var brokenTr = brokenPrefab.transform;
				brokenTr.position = thisTr.position;
				brokenTr.rotation = thisTr.rotation;
				brokenTr.localScale = thisTr.localScale;
				
				if (ObjectData.BreakAtCollisionPoint && LastHitPoint != null && brokenPrefab.TryGetComponent<Explode>(out var explode))
					explode.ExplosionPoint = LastHitPoint;

				brokenPrefab.SetActive(true);
			}
			
			Health = 0;
			
#if DEBUG_OBJ
			Debug.Log($"[Object {gameObject.name}] IObject broken by {source}");
#endif
			
			StateManager.Instance.RegisterDestroyedObject(ObjectID);
			Destroy(thisGo);
			enabled = false;
		}
		
		#endregion

		#region Pickupable

		public bool Pickupable { get; set; }

		private void initializePickupable()
		{
			Pickupable = false;
			
			if (ObjectData.PickupableAfter == 0f)
				Pickupable = true;
			else
				setPickupable().Forget();
		}
		
		private async UniTaskVoid setPickupable()
		{
			await UniTask.WaitForSeconds(ObjectData.PickupableAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			Pickupable = true;
		}
		
		public virtual bool CanPickup(IAlive user)
		{
			return ObjectData.IsPickupable && enabled && Pickupable && user.IsAlive;
		}
		
		public virtual bool Pickup(IAlive user)
		{
			if (!CanPickup(user))
				return false;
			
#if DEBUG_OBJ
			Debug.Log($"[Object {gameObject.name}] IObject picked up by {user.GetGameObject().name}");
#endif
			
			switch (ObjectData.PickupAction)
			{
				case EAction.None:
					return true;
				case EAction.DestroyGameObject:
					StateManager.Instance.RegisterDestroyedObject(ObjectID);
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
					StateManager.Instance.RegisterDestroyedComponent(ObjectID);
					Destroy(this);
					break;
			}

			enabled = false;
			return true;
		}

		public virtual void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			Pickup(alive);
		}
		
		#endregion
		
		#region Usable

		public bool Usable { get; set; }

		private void initializeUsable()
		{
			Usable = false;
			
			if (ObjectData.UsableAfter == 0f)
				Usable = true;
			else
				setUsable().Forget();
		}
		
		private async UniTaskVoid setUsable()
		{
			await UniTask.WaitForSeconds(ObjectData.UsableAfter);
			
			if (this == null || !isActiveAndEnabled)
				return;
			
			Usable = true;
		}
		
		public virtual bool CanUse(IAlive user)
		{
			return ObjectData.IsUsable && enabled && Usable && user.IsAlive;
		}
		
		public virtual bool Use(IAlive user)
		{
			if (!CanUse(user))
				return false;
			
#if DEBUG_OBJ
			Debug.Log($"[Object {gameObject.name}] IObject used by {user.GetGameObject().name}");
#endif
			
			switch (ObjectData.UseAction)
			{
				case EAction.None:
					return true;
				case EAction.DestroyGameObject:
					StateManager.Instance.RegisterDestroyedObject(ObjectID);
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
					StateManager.Instance.RegisterDestroyedComponent(ObjectID);
					Destroy(this);
					break;
			}
			
			enabled = false;
			return true;
		}

		#endregion
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;

		[JsonObject]
		public class BaseObjectState : IState
		{
			[JsonProperty]
			public float Health;

			[JsonProperty]
			public bool Pickupable;

			[JsonProperty]
			public bool Usable;

			public BaseObjectState() { }
			
			public BaseObjectState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not BaseObject baseObject)
					return;

				Health = baseObject.Health;
				Pickupable = baseObject.Pickupable;
				Usable = baseObject.Usable;
			}
			
			public void Apply(object obj)
			{
				if (obj is not BaseObject baseObject)
					return;

				baseObject.Health = Health;
				baseObject.Pickupable = Pickupable;
				baseObject.Usable = Usable;
			}
		}
	}
}