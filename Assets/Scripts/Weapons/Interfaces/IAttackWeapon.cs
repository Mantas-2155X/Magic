using System;
using Attacks.Enums;

namespace Weapons.Interfaces
{
	public interface IAttackWeapon
	{
		public EAttackAngle AttackAngle { get; }
		public Type Attack { get; }
	}
}