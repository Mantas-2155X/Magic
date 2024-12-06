//#define DEBUG_BaseProjectileWeapon

using Managers;
using Projectiles.Interfaces;
using Tools;
using UnityEngine;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon
	{
		public override bool Attack()
		{
			var success = base.Attack();
			if (!success)
				return false;

			if (Physics.Raycast(Ray, 1f, ~LayerMaskTools.Mask2))
			{
#if DEBUG_BaseProjectileWeapon
				Debug.Log($"[BaseProjectileWeapon {Owner.GetGameObject().name}] Too close to fire");
#endif
				return false;
			}

			if (Projectile != null)
			{
				IProjectile projectile;
				bool parent;

				var pooled = PoolingManager.Instance.TakeFromPool(Projectile, false);
				if (pooled != null)
				{
					projectile = pooled.GetComponent<IProjectile>();
					parent = false;
				}
				else
				{
					projectile = Instantiate(Resources.Load<GameObject>($"Projectiles/{Projectile.Name}")).GetComponent<IProjectile>();
					parent = true;
				}
				
				projectile.Spawn(this, Ray.origin + Ray.direction * 1f, Ray.direction * Force, parent);
			}
			
			return true;
		}
	}
}