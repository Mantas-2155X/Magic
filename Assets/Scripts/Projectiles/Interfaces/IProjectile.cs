using System;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Interfaces
{
	public interface IProjectile
	{
		public Rigidbody Rigidbody { get; }
		
		public IWeapon Source { get; }
		
		public float Lifetime { get; }
		public int Damage { get; }
		public Type Impact { get; }
		
		public void Spawn(IWeapon source, Vector3 origin, Vector3 force);
		public void Pool();

		public GameObject GetGameObject();
	}
}