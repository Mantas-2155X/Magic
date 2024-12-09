using System;
using Impacts.Interfaces;
using Projectiles.Interfaces;
using UnityEngine;
using Weapons.Interfaces;

namespace Managers
{
	public class ObjectManager : MonoBehaviour
	{
		public static ObjectManager Instance;

		public void Awake()
		{
			Instance = this;
		}

		public IProjectile CreateProjectile(Type type, IWeapon weapon, Vector3 origin, Vector3 force)
		{
			IProjectile projectile;
			bool parent;

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
			{
				projectile = pooled.GetComponent<IProjectile>();
				parent = false;
			}
			else
			{
				projectile = Instantiate(Resources.Load<GameObject>($"Projectiles/{type.Name}")).GetComponent<IProjectile>();
				parent = true;
			}
				
			projectile.Spawn(weapon, origin, force, parent);
			return projectile;
		}

		public IImpact CreateImpact(Type type, IProjectile projectile, Vector3 position, Vector3 angles)
		{
			IImpact impact;
			bool parent;
				
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
			{
				impact = pooled.GetComponent<IImpact>();
				parent = false;
			}
			else
			{
				impact = Instantiate(Resources.Load<GameObject>($"Impacts/{type.Name}")).GetComponent<IImpact>();
				parent = true;
			}
				
			impact.Spawn(projectile, position, angles, parent);
			return impact;
		}
	}
}