using System;
using AI.Interfaces;
using Attacks.Enums;
using Attacks.Interfaces;
using Casts.Interfaces;
using Objects;
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
		
		public IWeapon CreateWeapon(WeaponData data, Vector3 position, Vector3 angles)
		{
			IWeapon weapon;
				
			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				weapon = pooled.GetComponent<IWeapon>();
			else
				weapon = Instantiate(data.Prefab).GetComponent<IWeapon>();

			weapon.WeaponData = data;

			var go = weapon.GetGameObject();
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Dropped);
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			return weapon;
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
		
		public IProjectile CreateProjectile(ProjectileData data, IWeapon weapon, Vector3 origin, Vector3 direction)
		{
			IProjectile projectile;

			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				projectile = pooled.GetComponent<IProjectile>();
			else
				projectile = Instantiate(data.Prefab).GetComponent<IProjectile>();
			
			projectile.ProjectileData = data;
			projectile.Spawn(weapon, origin, direction * data.Force);
			return projectile;
		}

		public IAttack CreateAttack(AttackData data, Component source, RaycastHit hit, Transform attach)
		{
			return CreateAttack(data, source, hit.point, hit.normal, attach);
		}

		public IAttack CreateAttack(AttackData data, Component source, ContactPoint contact, Transform attach)
		{
			return CreateAttack(data, source, contact.point, contact.normal, attach);
		}
		
		public IAttack CreateAttack(AttackData data, Component source, Vector3 point, Vector3 normal, Transform attach)
		{
			IAttack attack;

			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				attack = pooled.GetComponent<IAttack>();
			else
				attack = Instantiate(data.Prefab).GetComponent<IAttack>();

			Quaternion angles;

			switch (data.AttackAngle)
			{
				case EAttackAngle.Identity:
					angles = Quaternion.identity;
					break;
				case EAttackAngle.HitNormal:
					angles = Quaternion.FromToRotation(Vector3.up, normal);
					break;
				case EAttackAngle.Owner:
					switch (source)
					{
						case IAlive alive:
							angles = alive.GetTransform().rotation;
							break;
						case IWeapon weapon:
							angles = weapon.Owner.GetTransform().rotation;
							break;
						case IProjectile projectile:
							angles = projectile.Source.Owner.GetTransform().rotation;
							break;
						default:
							angles = source.transform.rotation;
							break;
					}
					break;
				default:
					throw new NotImplementedException();
			}
			
			attack.AttackData = data;
			attack.Spawn(source, point, angles, attach);
			return attack;
		}
	}
}