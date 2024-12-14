using System;
using Attacks.Interfaces;
using Casts.Interfaces;
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

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				projectile = pooled.GetComponent<IProjectile>();
			else
				projectile = Instantiate(Resources.Load<GameObject>($"Projectiles/{type.Name}")).GetComponent<IProjectile>();
				
			projectile.Spawn(weapon, origin, force);
			return projectile;
		}
		
		public IWeapon CreateWeapon(Type type, Vector3 position, Vector3 angles)
		{
			IWeapon weapon;
				
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				weapon = pooled.GetComponent<IWeapon>();
			else
				weapon = Instantiate(Resources.Load<GameObject>($"Weapons/{type.Name}")).GetComponent<IWeapon>();

			var go = weapon.GetGameObject();
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Dropped);
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			return weapon;
		}
		
		public Portal CreatePortal(Vector3 position)
		{
			Portal portal;

			var pooled = PoolingManager.Instance.TakeFromPool(typeof(Portal), false);
			if (pooled != null)
				portal = pooled.GetComponent<Portal>();
			else
				portal = Instantiate(Resources.Load<GameObject>("Points/Portal")).GetComponent<Portal>();
			
			portal.Spawn(position);
			return portal;
		}
		
		public BasePool CreatePool(Type type, Vector3 position, float lifetime)
		{
			BasePool pool;
				
			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				pool = pooled.GetComponent<BasePool>();
			else
				pool = Instantiate(Resources.Load<GameObject>($"Pools/{type.Name}")).GetComponent<BasePool>();
			
			pool.Lifetime = lifetime;
			
			var go = pool.gameObject;
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Other);
			tr.position = position;
			
			go.SetActive(true);
			return pool;
		}
		
		public ICast CreateCast(Type type, Component source)
		{
			ICast cast;

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				cast = pooled.GetComponent<ICast>();
			else
				cast = Instantiate(Resources.Load<GameObject>($"Casts/{type.Name}")).GetComponent<ICast>();
			
			cast.Spawn(source);
			return cast;
		}
		
		public IAttack CreateAttack(Type type, Component source, Vector3 position, Quaternion angles)
		{
			IAttack attack;

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				attack = pooled.GetComponent<IAttack>();
			else
				attack = Instantiate(Resources.Load<GameObject>($"Attacks/{type.Name}")).GetComponent<IAttack>();
			
			attack.Spawn(source, position, angles);
			return attack;
		}
		
		public IAttack CreateAttack(Type type, Component source, Transform attach)
		{
			IAttack attack;

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				attack = pooled.GetComponent<IAttack>();
			else
				attack = Instantiate(Resources.Load<GameObject>($"Attacks/{type.Name}")).GetComponent<IAttack>();
			
			attack.Spawn(source, attach);
			return attack;
		}
	}
}