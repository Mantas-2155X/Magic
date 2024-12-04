using AI.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Projectiles.Interfaces
{
	public interface IProjectile
	{
		public Rigidbody Rigidbody { get; }
		
		public IWeapon Source { get; set; }
		
		public IAlive Owner { get; set; }

		public float Range { get; set; }

		public float Lifetime { get; set; }
		
		public int Damage { get; set; }

		public void Spawn(Vector3 origin, Vector3 force);
		
		public GameObject GetGameObject();
	}
}