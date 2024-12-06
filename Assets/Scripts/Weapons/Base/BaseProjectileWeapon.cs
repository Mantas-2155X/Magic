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
				Debug.Log($"[BaseProjectileWeapon {Owner.GetGameObject().name}] Too close to fire");
				return false;
			}

			if (Projectile != null)
			{
				var pooled = PoolingManager.Instance.TakeFromPool(Projectile, false);
				
				var projectile = pooled != null ? pooled.GetComponent<IProjectile>() : Instantiate(Resources.Load<GameObject>($"Projectiles/{Projectile.Name}")).GetComponent<IProjectile>();
				projectile.Spawn(this, Ray.origin + Ray.direction * 1f, Ray.direction * Force);
			}
			
			return true;
		}
	}
}