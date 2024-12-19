using System;
using Attacks;
using Attacks.Enums;
using Projectiles.Base;

namespace Projectiles
{
	public class FireBall : BaseProjectile
	{
		public override EAttackAngle AttackAngle => EAttackAngle.HitNormal;
	}
}