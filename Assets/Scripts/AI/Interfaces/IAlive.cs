using UnityEngine;
using Weapons.Interfaces;

namespace AI.Interfaces
{
	public interface IAlive
	{
		public Body Body { get; }
		
		public IWeapon Weapon { get; }

		public Color EyesColor { get; }
		
		public float CurrentSpeed { get; }
		public float MaximumSpeed { get; }

		public int CurrentHealth { get; }
		public int StartingHealth { get; }
		public int OverloadHealth { get; }
		
		public bool IsAlive { get; }
		public bool IsInvulnerable { get; }
		public bool IsNoclip { get; }
		public bool IsWalking { get; }
		
		public void SetInvulnerable(bool value);
		public void SetNoclip(bool value);
		
		public void TakeWeapon(IWeapon weapon);
		public void DropWeapon();

		public void Spawn(int startingHealth, int overloadHealth, float maximumSpeed);
		public void Heal(int health, object source);
		public void Damage(int damage, object source);
		public void Kill(object source);
		
		public bool IsGrounded();

		public GameObject GetGameObject();
	}
}