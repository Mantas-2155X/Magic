//#define DEBUG_BaseProjectileWeapon

using System;
using Managers;
using Tools;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon, IProjectileWeapon
	{
		[field: SerializeField]
		public virtual float Force { get; private set; }
		public virtual Type Projectile { get; private set; }

		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;

			if (Physics.Raycast(Ray, out var hit, 1f, ~LayerMaskTools.Mask2))
			{
#if DEBUG_BaseProjectileWeapon
				Debug.Log($"[BaseProjectileWeapon {Owner.GetGameObject().name}] Too close to fire, spawning at ray");
#endif
				ObjectManager.Instance.CreateProjectile(Projectile, this, hit.point, Ray.direction * 1f);
				return true;
			}

			ObjectManager.Instance.CreateProjectile(Projectile, this, Ray.origin + Ray.direction * 1f, Ray.direction * Force);
			return true;
		}
	}
}