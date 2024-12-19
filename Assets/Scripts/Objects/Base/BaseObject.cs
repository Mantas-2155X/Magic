using System.Runtime.CompilerServices;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Enums;
using Objects.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Base
{
	public class BaseObject : MonoBehaviour, IObject
	{
		[field: SerializeField]
		public ObjectData ObjectData { get; set; }
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

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

			PoolingManager.Instance.AddToPool(ObjectData, thisGo);
		}

		public virtual void OnParticleSystemStopped()
		{
			if (ObjectData.IsPoolable != EObjectPool.OnParticleSystemStopped)
				return;

			PoolingManager.Instance.AddToPool(ObjectData, thisGo);
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

		public float Health { get; private set; }
		public bool IsBroken { get; private set; }

		private void initializeBreakable()
		{
			Health = ObjectData.StartingHealth;
			IsBroken = false;
		}
		
		public virtual void Damage(float damage, object source)
		{
			if (!ObjectData.IsBreakable || IsBroken || damage < 0)
				return;
			
			Health -= damage;

			if (Health > 0)
				return;
			
			Break(source);
		}
		
		public virtual void Break(object source)
		{
			if (!ObjectData.IsBreakable || IsBroken)
				return;

			Health = 0;
			IsBroken = true;
			
			Destroy(thisGo);
			enabled = false;
		}
		
		#endregion

		#region Pickupable

		private bool pickupable;

		private void initializePickupable()
		{
			pickupable = false;
			
			if (ObjectData.PickupableAfter == 0f)
				pickupable = true;
			else
				setPickupable().Forget();
		}
		
		private async UniTaskVoid setPickupable()
		{
			await UniTask.WaitForSeconds(ObjectData.PickupableAfter);
			
			pickupable = true;
		}
		
		public virtual bool CanPickup(IAlive user)
		{
			return ObjectData.IsPickupable && enabled && pickupable && user.IsAlive;
		}
		
		public virtual bool Pickup(IAlive user)
		{
			if (!CanPickup(user))
				return false;
			
			switch (ObjectData.PickupAction)
			{
				case EAction.None:
					return true;
				case EAction.DestroyGameObject:
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
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

		private bool usable;

		private void initializeUsable()
		{
			usable = false;
			
			if (ObjectData.UsableAfter == 0f)
				usable = true;
			else
				setUsable().Forget();
		}
		
		private async UniTaskVoid setUsable()
		{
			await UniTask.WaitForSeconds(ObjectData.UsableAfter);
			
			usable = true;
		}
		
		public virtual bool CanUse(IAlive user)
		{
			return ObjectData.IsUsable && enabled && usable && user.IsAlive;
		}
		
		public virtual bool Use(IAlive user)
		{
			if (!CanUse(user))
				return false;
			
			switch (ObjectData.UseAction)
			{
				case EAction.None:
					return true;
				case EAction.DestroyGameObject:
					Destroy(thisGo);
					break;
				case EAction.DestroyComponent:
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