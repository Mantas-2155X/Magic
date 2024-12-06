using System;
using Projectiles;
using Weapons.Base;

namespace Weapons
{
	public class FireGun : BaseProjectileWeapon
	{
		public override Type Projectile => typeof(FireBall);
	}
}