using System;
using Attacks.Enums;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Interfaces
{
	public interface IProjectile
	{
		public ProjectileData ProjectileData { get; set; }

		public IWeapon Source { get; }

		public Rigidbody Rigidbody { get; }
		public Collider Collider { get; }
		
		public EAttackAngle AttackAngle { get; }
		public Type Attack { get; }
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force);

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}