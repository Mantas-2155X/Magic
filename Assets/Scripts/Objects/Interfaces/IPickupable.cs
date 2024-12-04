using AI.Interfaces;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IPickupable
	{
		public string[] PickupLayers { get; }
		
		public bool DestroyAfterPickup { get; }
		
		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		public GameObject GetGameObject();
	}
}