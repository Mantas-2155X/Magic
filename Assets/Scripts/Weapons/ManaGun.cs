using System;
using Attacks;
using Weapons.Base;

namespace Weapons
{
	public class ManaGun : BaseAttackWeapon
	{
		public override Type Attack => typeof(ManaPool);
	}
}