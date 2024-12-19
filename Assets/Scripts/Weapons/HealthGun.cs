using System;
using Attacks;
using Weapons.Base;

namespace Weapons
{
	public class HealthGun : BaseAttackWeapon
	{
		public override Type Attack => typeof(HealthPool);
	}
}