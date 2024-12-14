using System;
using Projectiles.Base;

namespace Projectiles
{
	public class FireBall : BaseProjectile
	{
		public override Type Impact => typeof(Impacts.FireSpark);
	}
}