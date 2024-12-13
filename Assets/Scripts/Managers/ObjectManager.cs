using System;
using Casts.Interfaces;
using Impacts.Interfaces;
using Objects;
using Objects.Base;
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
		
		public IWeapon CreateWeapon(Type type, Vector3 position, Vector3 angles)
		{
			IWeapon weapon;
			bool parent;
				
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
			{
				weapon = pooled.GetComponent<IWeapon>();
				parent = false;
			}
			else
			{
				weapon = Instantiate(Resources.Load<GameObject>($"Weapons/{type.Name}")).GetComponent<IWeapon>();
				parent = true;
			}

			var go = weapon.GetGameObject();
			var tr = go.transform;

			if (parent)
				tr.SetParent(World.World.Instance.Dropped);
			
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			return weapon;
		}
		
		public Portal CreatePortal(Vector3 position)
		{
			Portal portal;
			bool parent;

			var pooled = PoolingManager.Instance.TakeFromPool(typeof(Portal), false);
			if (pooled != null)
			{
				portal = pooled.GetComponent<Portal>();
				parent = false;
			}
			else
			{
				portal = Instantiate(Resources.Load<GameObject>("Points/Portal")).GetComponent<Portal>();
				parent = true;
			}
			
			if (parent)
				portal.transform.SetParent(World.World.Instance.Other);

			portal.Spawn(position);
			return portal;
		}
		
		public BasePool CreatePool(Type type, Vector3 position, float lifetime)
		{
			BasePool pool;
			bool parent;
				
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
			{
				pool = pooled.GetComponent<BasePool>();
				parent = false;
			}
			else
			{
				pool = Instantiate(Resources.Load<GameObject>($"Objects/{type.Name}")).GetComponent<BasePool>();
				parent = true;
			}

			pool.Lifetime = lifetime;
			
			var go = pool.gameObject;
			var tr = go.transform;

			if (parent)
				tr.SetParent(World.World.Instance.Objects);
			
			tr.position = position;
			
			go.SetActive(true);
			return pool;
		}
		
		public ICast CreateCast(Type type, IWeapon weapon)
		{
			ICast cast;
			
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
			{
				cast = pooled.GetComponent<ICast>();
			}
			else
			{
				cast = Instantiate(Resources.Load<GameObject>($"Casts/{type.Name}")).GetComponent<ICast>();
			}
			
			cast.Spawn(weapon);
			return cast;
		}
	}
}