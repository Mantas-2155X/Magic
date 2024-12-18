using System.Runtime.CompilerServices;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Objects.Enums;
using Objects.Interfaces;
using UnityEngine;

namespace Objects.Base
{
	public class BasePickupable : MonoBehaviour, IPickupable
	{
		[field: SerializeField]
		public virtual float PickupableAfter { get; private set; }
		[field: SerializeField]
		public virtual EDestroyType DestroyAfterPickup { get; private set; }

		private bool destroyed;
		private bool pickupable;
		
		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		public void Awake()
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				init = true;
			}
		}
		
		public virtual void OnEnable()
		{
			if (PickupableAfter == 0f)
				pickupable = true;
			else
				setPickupable().Forget();
		}

		public virtual bool CanPickup(IAlive user)
		{
			return !destroyed && pickupable && user.IsAlive;
		}
		
		public virtual bool Pickup(IAlive user)
		{
			if (!CanPickup(user))
				return false;
			
			switch (DestroyAfterPickup)
			{
				case EDestroyType.None:
					return true;
				case EDestroyType.GameObject:
					destroyed = true;
					Destroy(thisGo);
					break;
				case EDestroyType.Component:
					destroyed = true;
					Destroy(this);
					break;
			}
			
			return true;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		public void OnTriggerEnter(Collider other)
		{
			if (!AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			Pickup(alive);
		}

		private async UniTaskVoid setPickupable()
		{
			await UniTask.WaitForSeconds(PickupableAfter);
			
			pickupable = true;
		}
	}
}