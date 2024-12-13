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

			Vector3 origin;
			Vector3 force;
			
			if (Physics.Raycast(FinishedRay, out var hit, 1f, ~LayerMaskTools.Mask2))
			{
#if DEBUG_BaseProjectileWeapon
				Debug.Log($"[BaseProjectileWeapon {Owner.GetGameObject().name}] Too close to fire, spawning at ray");
#endif
				origin = hit.point;
				force = FinishedRay.direction * 1f;
			}
			else
			{
				origin = FinishedRay.origin + FinishedRay.direction * 1f;
				force = FinishedRay.direction * Force;
			}

			ObjectManager.Instance.CreateProjectile(Projectile, this, origin, force);
		}
	}
}