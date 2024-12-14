using System;
using Managers;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon, IProjectileWeapon
	{
		[field: SerializeField]
		public virtual float Force { get; private set; }
		public virtual Type Projectile { get; private set; }

		public override void FinishCasting()
		{
			base.FinishCasting();

			var origin = LastRay.origin + LastRay.direction * 1f;
			var force = LastRay.direction * Force;

			ObjectManager.Instance.CreateProjectile(Projectile, this, origin, force);
		}
	}
}