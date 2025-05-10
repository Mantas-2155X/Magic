//#define DEBUG_OBJ

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Enums;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Enums;
using Objects.Interfaces;
using ScriptableObjects;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Objects.Base
{
	public class BaseObject : MonoBehaviour, IObject
	{
		[field: SerializeField]
		public ObjectData ObjectData { get; set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }

		public virtual bool ShouldSave => true;
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		#region Identify / SaveLoad

		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var transformState = TransformState.Read(thisTr);
			if (transformState != null)
				dict[typeof(Transform).ToString()] = JObject.FromObject(transformState);

			var rigidbodyState = RigidbodyState.Read(Rigidbody);
			if (rigidbodyState != null)
				dict[typeof(Rigidbody).ToString()] = JObject.FromObject(rigidbodyState);

			var baseObjectState = BaseObjectState.Read(this);
			if (baseObjectState != null)
				dict[typeof(BaseObject).ToString()] = JObject.FromObject(baseObjectState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState))
				TransformState.Apply(thisTr, transformState.ToObject<TransformState>());
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState))
				RigidbodyState.Apply(Rigidbody, rigidbodyState.ToObject<RigidbodyState>());
			
			if (data.TryGetValue(typeof(BaseObject).ToString(), out var baseObjectState))
				BaseObjectState.Apply(this, baseObjectState.ToObject<BaseObjectState>());
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
		
		public virtual void Damage(float damage, object source, EElement type)
		{
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

			if (ObjectData.BrokenPrefab != null && SettingsManager.Instance.GetBool("graphics-shatterobjects") == true)
			{
				var brokenPrefab = Instantiate(ObjectData.BrokenPrefab, World.World.Instance.Ragdolls);
				
				var brokenTr = brokenPrefab.transform;
				brokenTr.position = thisTr.position;
				brokenTr.rotation = thisTr.rotation;
				brokenTr.localScale = thisTr.localScale;
				
				brokenPrefab.SetActive(true);
			}
			
			Health = 0;
			
#if DEBUG_OBJ
			Debug.Log($"[Object {gameObject.name}] IObject broken by {source}");
#endif
			
			StateManager.Instance.DestroyedObjects.AddUnique(ObjectID);
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
					StateManager.Instance.DestroyedObjects.AddUnique(ObjectID);
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
					StateManager.Instance.DestroyedComponents.AddUnique(ObjectID);
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
					StateManager.Instance.DestroyedObjects.AddUnique(ObjectID);
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
					StateManager.Instance.DestroyedComponents.AddUnique(ObjectID);
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
	}
}