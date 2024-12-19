using System;
using Attacks;
using Attacks.Enums;
using Casts;
using Weapons.Base;

namespace Weapons
{
	public class ShieldGun : BaseAttackWeapon
	{
		public override Type Attack => typeof(Shield);
		public override EAttackAngle AttackAngle => EAttackAngle.Owner;
	}
}