using System;
using Attacks;
using Casts;
using Weapons.Base;

namespace Weapons
{
	public class BindGun : BaseAttackWeapon
	{
		public override Type Cast => typeof(ManaRing);
		public override Type Attack => typeof(Bind);
	}
}