using Weapons.Base;

namespace Weapons
{
	public class FireGun : BaseProjectileWeapon
	{
		public override float TimeBetweenAttacks => 0.25f;

		public override string Projectile => "FireBall";
		public override float Force => 7.5f;
	}
}