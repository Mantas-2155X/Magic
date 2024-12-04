using AI.Interfaces;
using UnityEngine;

namespace Weapons.Interfaces
{
	public interface IWeapon
	{
		public IAlive Owner { get; }

		public float Force { get; }
		public string Projectile { get; }
		public float TimeBetweenAttacks { get; }

		public void Take(IAlive alive);
		public void Drop();
		
		public bool CanAttack();
		public bool Attack();
		
		public GameObject GetGameObject();
	}
}