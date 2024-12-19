using System;
using System.Collections.Generic;
using AI.Interfaces;
using Attacks.Enums;
using Attacks.Interfaces;
using Casts.Interfaces;
using Objects.Interfaces;
using Projectiles.Interfaces;
using ScriptableObjects;
using UnityEngine;
using Weapons.Interfaces;

namespace Managers
{
	public class ObjectManager : MonoBehaviour
	{
		public static ObjectManager Instance;

		private readonly Dictionary<string, Data> datasMap = new ();

		private readonly string[] dataPaths = { "Objects", "Weapons", "Casts", "Projectiles", "Attacks" };
		
		public void Awake()
		{
			Instance = this;
			setupDatasMap();
		}

		#region Init

		private void setupDatasMap()
		{
			foreach (var dataPath in dataPaths)
			{
				var datas = Resources.LoadAll<Data>(dataPath);

				for (var i = 0; i < datas.Length; i++)
				{
					var data = datas[i];
					datasMap[$"{dataPath}/{data.Name}"] = data;
				}
			}
		}

		#endregion
		
		#region Get

		public ObjectData GetObject(string path)
		{
			return (ObjectData)datasMap.GetValueOrDefault($"Objects/{path}");
		}
		
		public WeaponData GetWeapon(string path)
		{
			return (WeaponData)datasMap.GetValueOrDefault($"Weapons/{path}");
		}
		
		public CastData GetCast(string path)
		{
			return (CastData)datasMap.GetValueOrDefault($"Casts/{path}");
		}
		
		public ProjectileData GetProjectile(string path)
		{
			return (ProjectileData)datasMap.GetValueOrDefault($"Projectiles/{path}");
		}
		
		public AttackData GetAttack(string path)
		{
			return (AttackData)datasMap.GetValueOrDefault($"Attacks/{path}");
		}
		
		#endregion

		#region Create

		public IObject CreateObject(ObjectData data, Vector3 position, Vector3 angles)
		{
			IObject obj;

			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				obj = pooled.GetComponent<IObject>();
			else
				obj = Instantiate(data.Prefab).GetComponent<IObject>();
			
			obj.Spawn(position, angles);
			return obj;
		}
		
		public IWeapon CreateWeapon(WeaponData data, Vector3 position, Vector3 angles)
		{
			IWeapon weapon;
				
			var pooled = PoolingManager.Instance.TakeFromPool(data, false);
			if (pooled != null)
				weapon = pooled.GetComponent<IWeapon>();
			else
				weapon = Instantiate(data.Prefab).GetComponent<IWeapon>();
			
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
			
			projectile.Spawn(weapon, origin, direction * data.Force);
			return projectile;
		}

		public IAttack CreateAttack(AttackData data, Component source, RaycastHit hit, Transform attach)
		{
			return createAttack(data, source, hit.point, hit.normal, attach);
		}

		public IAttack CreateAttack(AttackData data, Component source, ContactPoint contact, Transform attach)
		{
			return createAttack(data, source, contact.point, contact.normal, attach);
		}
		
		private IAttack createAttack(AttackData data, Component source, Vector3 point, Vector3 normal, Transform attach)
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
			
			attack.Spawn(source, point, angles, attach);
			return attack;
		}
		
		#endregion
	}
}