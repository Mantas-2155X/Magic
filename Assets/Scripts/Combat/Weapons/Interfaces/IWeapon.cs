using AI.Interfaces;
using Objects;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Weapons.Interfaces
{
	public interface IWeapon
	{
		public WeaponData WeaponData { get; }
		
		public IAlive Owner { get; }
		
		public Rigidbody Rigidbody { get; }
		public Collider[] Colliders { get; }
		public DroppedWeapon DroppedWeapon { get; }

		public void Spawn(Vector3 position, Vector3 angles);

		public void Take(IAlive alive);
		public void Drop();

		public IAlive GetAlive();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}