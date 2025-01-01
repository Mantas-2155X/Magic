using AI.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Projectiles.Interfaces
{
	public interface IProjectile
	{
		public ProjectileData ProjectileData { get; }

		public Component Source { get; }

		public Rigidbody Rigidbody { get; }
		public Collider Collider { get; }
		
		public void Spawn(Component source, float range, AttackData attack, Vector3 origin, Vector3 force);

		public IAlive GetAlive();

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}