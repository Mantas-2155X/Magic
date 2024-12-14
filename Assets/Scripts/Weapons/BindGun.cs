using System;
using Casts;
using Projectiles;
using Weapons.Base;

namespace Weapons
{
	public class BindGun : BaseProjectileWeapon
	{
		public override Type Cast => typeof(ManaRing);
		public override Type Projectile => typeof(ManaBall);
	}
}