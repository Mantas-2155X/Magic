using AI.Interfaces;
using Objects.Enums;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IPickupable
	{
		public string DisplayName { get; }
		
		public float PickupableAfter { get; }
		public EDestroyType DestroyAfterPickup { get; }
		
		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}