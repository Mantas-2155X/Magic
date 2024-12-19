using System;
using Attacks.Enums;
using ScriptableObjects;

namespace Weapons.Interfaces
{
	public interface IAttackWeapon
	{
		public EAttackAngle AttackAngle { get; }
		public AttackData Attack { get; }
	}
}