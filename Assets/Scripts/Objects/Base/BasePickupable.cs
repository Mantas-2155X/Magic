using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Objects.Interfaces;
using Tools;
using UnityEngine;

namespace Objects.Base
{
	public class BasePickupable : MonoBehaviour, IPickupable
	{
		[SerializeField]
		public float PickupableAfter = 1;

		public virtual string[] PickupLayers { get; private set; } = {"NPC", "Player"};
		public virtual bool DestroyAfterPickup { get; private set; } = true;

		private LayerMask filterMask;
		private bool pickupable;

		public void Awake()
		{
			filterMask = LayerMask.GetMask(PickupLayers);
			setPickupable().Forget();
		}

		public virtual bool CanPickup(IAlive user)
		{
			return user.IsAlive;
		}
		
		public virtual bool Pickup(IAlive user)
		{
			if (!CanPickup(user))
				return false;
			
			if (!DestroyAfterPickup)
				return true;

			Destroy(gameObject);
			return true;
		}

		public GameObject GetGameObject()
		{
			return gameObject;
		}
		
		public void OnTriggerEnter(Collider other)
		{
			if (!pickupable)
				return;
			
			var alive = other.GetComponent<IAlive>();
			if (alive == null || !filterMask.ContainsLayer(other.gameObject.layer))
				return;

			Pickup(alive);
		}

		private async UniTask setPickupable()
		{
			await UniTask.WaitForSeconds(PickupableAfter);
			
			if (this == null)
				return;

			pickupable = true;
		}
	}
}