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
			
			var pj = Instantiate(Resources.Load<GameObject>($"Projectiles/{Projectile}")).GetComponent<IProjectile>();
			pj.Spawn(this, Ray.origin + Ray.direction * 1f, Ray.direction * Force);
			
			return true;
		}
	}
}