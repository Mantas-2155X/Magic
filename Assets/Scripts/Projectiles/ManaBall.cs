using System;
using Attacks;
using Projectiles.Base;

namespace Projectiles
{
	public class ManaBall : BaseProjectile
	{
		public override Type Attack => typeof(Bind);
	}
}