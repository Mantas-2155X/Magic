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

		public override void FinishCasting()
		{
			base.FinishCasting();
			
			if (Physics.Raycast(FinishedRay, out var hit, 1f, ~LayerMaskTools.Mask2))
			{
#if DEBUG_BaseProjectileWeapon
				Debug.Log($"[BaseProjectileWeapon {Owner.GetGameObject().name}] Too close to fire, spawning at ray");
#endif
				ObjectManager.Instance.CreateProjectile(Projectile, this, hit.point, FinishedRay.direction * 1f);
				return;
			}

			ObjectManager.Instance.CreateProjectile(Projectile, this, FinishedRay.origin + FinishedRay.direction * 1f, FinishedRay.direction * Force);
		}
	}
}