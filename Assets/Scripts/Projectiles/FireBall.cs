using System;
using Projectiles.Base;

namespace Projectiles
{
	public class FireBall : BaseProjectile
	{
		public override float Distance => 10f;
		public override float Damage => 10f;
		public override Type Impact => typeof(Impacts.FireSpark);
	}
}