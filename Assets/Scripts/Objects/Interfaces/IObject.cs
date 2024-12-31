using AI.Interfaces;
using Combat.Enums;
using ScriptableObjects;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IObject
	{
		public ObjectData ObjectData { get; set; }

		#region Breakable

		public float Health { get; }
		public bool IsBroken { get; }

		public void Damage(float damage, object source, EElement type);
		public void Break(object source);

		#endregion

		#region Pickupable

		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		#endregion

		#region Usable

		public bool CanUse(IAlive user);
		public bool Use(IAlive user);
		
		#endregion
		
		public void Spawn(Vector3 position, Vector3 angles);
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}