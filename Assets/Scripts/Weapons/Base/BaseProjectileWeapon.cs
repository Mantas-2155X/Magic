using Projectiles.Interfaces;
using Tools;
using UnityEngine;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon
	{
		public virtual string Projectile { get; set; }
		public virtual float Force { get; set; }

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
			
			var go = Instantiate(Resources.Load<GameObject>($"Projectiles/{Projectile}"));

			var pj = go.GetComponent<IProjectile>();
			pj.Source = this;
			pj.Owner = Owner;
			pj.Spawn(Ray.origin + Ray.direction * 1f, Ray.direction * Force);
			
			return true;
		}
	}
}