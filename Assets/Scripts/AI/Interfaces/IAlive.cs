using UnityEngine;
using Weapons.Interfaces;

namespace AI.Interfaces
{
	public interface IAlive
	{
		public Body Body { get; }
		
		public IWeapon Weapon { get; }
		
		public float CurrentSpeed { get; }
		public float MaximumSpeed { get; }

		public float CurrentHealth { get; }
		public float StartingHealth { get; }
		public float OverloadHealth { get; }

		public float CurrentMana { get; }
		public float StartingMana { get; }
		public float OverloadMana { get; }

		public bool IsAlive { get; }
		public bool IsInvulnerable { get; }
		public bool IsPowerful { get; }
		public bool IsNoclip { get; }
		public bool IsWalking { get; }
		
		public void SetInvulnerable(bool value);
		public void SetPowerful(bool value);
		public void SetNoclip(bool value);
		
		public void TakeWeapon(IWeapon weapon);
		public void DropWeapon();

		public void Spawn(float startingHealth, float overloadHealth, float startingMana, float overloadMana, float maximumSpeed);
		public void Heal(float health, object source);
		public void Damage(float damage, object source);
		public void GenerateMana(float mana, object source);
		public void UseMana(float mana, object source);
		public void Kill(object source);
		
		public bool IsGrounded();

		public GameObject GetGameObject();
	}
}