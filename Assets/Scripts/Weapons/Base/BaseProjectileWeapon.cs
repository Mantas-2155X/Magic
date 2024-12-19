using System;
using Managers;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon, IProjectileWeapon
	{
		[field: SerializeField]
		public virtual float Force { get; private set; }

		[field: SerializeField]
		public ProjectileData Projectile { get; private set; }

		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;

			var origin = LastRay.origin;
			var force = LastRay.direction * Force;

			ObjectManager.Instance.CreateProjectile(Projectile, this, origin, force);
			return true;
		}
	}
}