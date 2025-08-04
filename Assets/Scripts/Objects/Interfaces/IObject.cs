using AI.Interfaces;
using Combat.Enums;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IObject : ISaveable
	{
		public ObjectData ObjectData { get; set; }

		public Rigidbody Rigidbody { get; set; }

		#region Breakable

		public float Health { get; }

		public void Damage(float damage, object source, EElement type, Vector3? hitPoint = null);
		public void Break(object source);

		#endregion

		#region Pickupable

		public bool Pickupable { get; }

		public bool CanPickup(IAlive user);
		public bool Pickup(IAlive user);
		
		#endregion

		#region Usable

		public bool Usable { get; }

		public bool CanUse(IAlive user);
		public bool Use(IAlive user);
		
		#endregion
		
		public void Spawn(Vector3 position, Vector3 angles);
	}
}