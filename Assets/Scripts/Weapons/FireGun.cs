using System;
using Casts;
using Projectiles;
using Weapons.Base;

namespace Weapons
{
	public class FireGun : BaseProjectileWeapon
	{
		public override Type Cast => typeof(FireRing);
		public override Type Projectile => typeof(FireBall);
	}
}