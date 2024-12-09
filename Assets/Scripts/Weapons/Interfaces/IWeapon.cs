using AI.Interfaces;
using UnityEngine;

namespace Weapons.Interfaces
{
	public interface IWeapon
	{
		public IAlive Owner { get; }
		
		public Collider[] Colliders { get; }

		public float TimeBetweenAttacks { get; }

		public void Take(IAlive alive);
		public void Drop();
		
		public bool CanAttack();
		public bool Attack();
		
		public GameObject GetGameObject();
	}
}