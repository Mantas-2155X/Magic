using Projectiles.Base;

namespace Projectiles
{
	public class FireBall : BaseProjectile
	{
		public override float Range => 11f;
		
		public override float Lifetime => 1f;

		public override int Damage => 10;
		
		public override string Impact => "FireBall";
	}
}