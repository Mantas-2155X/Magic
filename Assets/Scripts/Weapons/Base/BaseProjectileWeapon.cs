using Managers;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseProjectileWeapon : BaseWeapon, IProjectileWeapon
	{
		[field: SerializeField]
		public ProjectileData Projectile { get; private set; }

		public override bool FinishCasting()
		{
			var status = base.FinishCasting();
			if (!status)
				return false;

			ObjectManager.Instance.CreateProjectile(Projectile, this, LastRay.origin, LastRay.direction);
			return true;
		}
	}
}