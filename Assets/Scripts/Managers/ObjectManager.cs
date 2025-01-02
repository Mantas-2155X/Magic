using System;
using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Enums;
using Combat.Attacks.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Combat.Wearables.Interfaces;
using Objects.Interfaces;
using ScriptableObjects;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Managers
{
	public class ObjectManager : MonoBehaviour
	{
		public static ObjectManager Instance;

		private readonly Dictionary<string, Data> datasMap = new ();
		private readonly List<IObject> activeObjects = new ();

		private readonly string[] dataPaths = { "Objects", "Wearables", "Casts", "Projectiles", "Attacks", "Spells", "AI" };

		public ObjectManager()
		{
			Instance = this;
		}
		
		public void Awake()
		{
			setupDatasMap();
		}

		#region Init

		private void setupDatasMap()
		{
			foreach (var dataPath in dataPaths)
			{
				var datas = Addressables.LoadAssetsAsync<Data>(dataPath).WaitForCompletion();

				for (var i = 0; i < datas.Count; i++)
				{
					var data = datas[i];
					datasMap[$"{dataPath}/{data.Name}"] = data;
				}
			}
		}

		#endregion

		#region Registry

		public void Register(IObject obj)
		{
			if (activeObjects.Contains(obj))
				return;
			
			activeObjects.Add(obj);
		}

		public void Unregister(IObject obj)
		{
			activeObjects.Remove(obj);
		}

		public List<IObject> GetRegisteredObjects()
		{
			return activeObjects;
		}

		#endregion
		
		#region Get

		public ObjectData GetObject(string path)
		{
			return (ObjectData)datasMap.GetValueOrDefault($"Objects/{path}");
		}
		
		public WearableData GetWearable(string path)
		{
			return (WearableData)datasMap.GetValueOrDefault($"Wearables/{path}");
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
		
		public SpellData GetSpell(string path)
		{
			return (SpellData)datasMap.GetValueOrDefault($"Spells/{path}");
		}
		
		public AliveData GetAlive(string path)
		{
			return (AliveData)datasMap.GetValueOrDefault($"AI/{path}");
		}
		
		#endregion

		#region Create

		public IObject CreateObject(ObjectData data, Vector3 position, Vector3 angles)
		{
			var obj = PoolingManager.Instance.TakeOrCreate<IObject>(data, false);
			obj.Spawn(position, angles);
			
			return obj;
		}
		
		public IWearable CreateWearable(WearableData data, Vector3 position, Vector3 angles)
		{
			var wearable = PoolingManager.Instance.TakeOrCreate<IWearable>(data, false);
			wearable.Spawn(position, angles);
			
			return wearable;
		}
		
		public ICast CreateCast(CastData data, Component source)
		{
			var cast = PoolingManager.Instance.TakeOrCreate<ICast>(data, false);
			cast.Spawn(source);
			
			return cast;
		}
		
		public IProjectile CreateProjectile(ProjectileData data, float range, AttackData attack, Component source, Vector3 origin, Vector3 direction)
		{
			var projectile = PoolingManager.Instance.TakeOrCreate<IProjectile>(data, false);
			projectile.Spawn(source, range, attack, origin, direction * data.Force);
			
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
		
		public IAttack CreateAttack(AttackData data, Component source, Transform attach)
		{
			return createAttack(data, source, Vector3.zero, Vector3.zero, attach);
		}
		
		private IAttack createAttack(AttackData data, Component source, Vector3 point, Vector3 normal, Transform attach)
		{
			var attack = PoolingManager.Instance.TakeOrCreate<IAttack>(data, false);

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
						case ISpell spell:
							angles = spell.Owner.GetTransform().rotation;
							break;
						case IAttack srcAttack:
							angles = srcAttack.GetAlive().GetTransform().rotation;
							break;
						case IProjectile projectile:
							angles = projectile.GetAlive().GetTransform().rotation;
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