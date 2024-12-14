using System;

namespace Weapons.Interfaces
{
	public interface IAttackWeapon
	{
		public bool Attach { get; }
		public float Distance { get; }
		public Type Attack { get; }
	}
}