using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Projectiles.Interfaces
{
	public interface IProjectile : IIdentifiable
	{
		public ProjectileData ProjectileData { get; }

		public IIdentifiable Source { get; }

		public Rigidbody Rigidbody { get; }
		public Collider Collider { get; }
		
		public void Spawn(IIdentifiable source, float range, AttackData attack, Vector3 origin, Vector3 force);

		public IAlive GetAlive();
	}
}