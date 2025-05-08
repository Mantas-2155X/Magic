using AI.Interfaces;
using Objects;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Wearables.Interfaces
{
	public interface IWearable
	{
		public WearableData WearableData { get; }
		
		public string ObjectID { get; set; }

		public IAlive Owner { get; }
		
		public Rigidbody Rigidbody { get; }
		public Collider[] Colliders { get; }

		public void Spawn(Vector3 position, Vector3 angles);

		public void Equip(IAlive alive);
		public void Drop();

		public IAlive GetAlive();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}