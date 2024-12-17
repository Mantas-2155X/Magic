using AI.Interfaces;
using Cysharp.Threading.Tasks;
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
					Destroy(gameObject);
					break;
				case EDestroyType.Component:
					destroyed = true;
					Destroy(this);
					break;
			}
			
			return true;
		}

		public GameObject GetGameObject()
		{
			return gameObject;
		}
		
		public void OnTriggerEnter(Collider other)
		{
			var alive = other.GetComponent<IAlive>();
			if (alive == null)
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