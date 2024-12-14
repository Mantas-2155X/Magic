using System;
using Attacks;
using Casts;
using Weapons.Base;

namespace Weapons
{
	public class Destroyer : BaseAttackWeapon
	{
		public override Type Cast => typeof(FireRing);
		public override Type Attack => typeof(Incinerate);
	}
}