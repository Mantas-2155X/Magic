using System;

namespace Weapons.Interfaces
{
	public interface IAttackWeapon
	{
		public float Distance { get; }
		public Type Attack { get; }
	}
}