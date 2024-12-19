using System;
using Attacks.Base;
using Attacks.Interfaces;
using Casts.Interfaces;
using Objects;
using Objects.Base;
using Projectiles.Interfaces;
using ScriptableObjects;
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
		
		public ICast CreateCast(CastData data, Component source)
		{
			ICast cast;

			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				cast = pooled.GetComponent<ICast>();
			else
				cast = Instantiate(data.Prefab).GetComponent<ICast>();

			cast.CastData = data;
			cast.Spawn(source);
			return cast;
		}
		
		public IAttack CreateAttack(Type type, Component source, Vector3 position, Quaternion angles, Transform attach)
		{
			IAttack attack;

			var pooled = PoolingManager.Instance.TakeFromPool(type, false);
			if (pooled != null)
				attack = pooled.GetComponent<IAttack>();
			else
				attack = Instantiate(Resources.Load<GameObject>($"Attacks/{type.Name}")).GetComponent<IAttack>();
			
			attack.Spawn(source, position, angles, attach);
			return attack;
		}
	}
}