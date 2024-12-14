using System;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Interfaces
{
	public interface IProjectile
	{
		public IWeapon Source { get; }

		public Rigidbody Rigidbody { get; }
		public Collider Collider { get; }
		
		public float Distance { get; }
		public float Damage { get; }
		public bool UseNormalAngle { get; }
		
		public Type Attack { get; }
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force);

		public GameObject GetGameObject();
	}
}