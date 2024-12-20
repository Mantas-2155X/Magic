using AI.Interfaces;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Interfaces
{
	public interface IProjectile
	{
		public ProjectileData ProjectileData { get; }

		public IWeapon Source { get; }

		public Rigidbody Rigidbody { get; }
		public Collider Collider { get; }
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force);

		public IAlive GetAlive();

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}