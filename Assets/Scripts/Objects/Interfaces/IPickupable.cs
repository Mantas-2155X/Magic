using AI.Interfaces;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IPickupable
	{
		public LayerMask PickupLayers { get; }
		public float PickupableAfter { get; }
		public bool DestroyAfterPickup { get; }
		
		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		public GameObject GetGameObject();
	}
}