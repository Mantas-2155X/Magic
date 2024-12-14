using System;
using Attacks;
using Projectiles.Base;

namespace Projectiles
{
	public class FireBall : BaseProjectile
	{
		public override Type Attack => typeof(FireSpark);
	}
}