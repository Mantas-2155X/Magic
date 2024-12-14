using System;
using Attacks.Enums;

namespace Weapons.Interfaces
{
	public interface IAttackWeapon
	{
		public float Distance { get; }
		
		public EAttackAngle AttackAngle { get; }
		public Type Attack { get; }
	}
}