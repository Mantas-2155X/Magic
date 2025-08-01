//#define PRINT_DATAS

using System;
using System.Collections.Generic;
using AI.Interfaces;
using Combat.Attacks.Enums;
using Combat.Attacks.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Decals.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Combat.Wearables.Interfaces;
using Modding;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Managers
{
	public class ObjectManager
	{
		private static ObjectManager instance;
		public static ObjectManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new ObjectManager();
				instance.setupDatasMap();
				_ = ModLoader.Instance;
#if PRINT_DATAS
				instance.printDatasMap();
#endif
				
				return instance;
			}
		}

		private readonly Dictionary<string, Data> datasMap = new ();
		private readonly Dictionary<string, Data> moddedDatasMap = new ();
		
		private readonly string[] dataPaths = { "Objects", "Wearables", "Casts", "Projectiles", "Attacks", "Spells", "AI", "Decals", "Scenes", "Paths" };

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

		private void printDatasMap()
		{
			Debug.Log("[ContentManager] Printing datas map");

			foreach (var pair in datasMap)
				Debug.Log($"[ContentManager] {pair.Value.GetType().Name} - {pair.Key}");
			
			Debug.Log("[ContentManager] Printing modded datas map");

			foreach (var pair in moddedDatasMap)
				Debug.Log($"[ContentManager] {pair.Value.GetType().Name} - {pair.Key}");
		}
		
		#endregion

		#region Modify

		internal void SetModdedDataMap(string key, Data value)
		{
			moddedDatasMap[key] = value;
		}

		internal void RemoveModdedDataMap(string key)
		{
			moddedDatasMap.Remove(key);
		}
		
		#endregion
		
		#region Get

		public Data GetData(string path)
		{
			return datasMap.TryGetValue(path, out var data) ? data : moddedDatasMap.GetValueOrDefault(path);
		}
		
		public ObjectData GetObject(string path)
		{
			return (ObjectData)GetData($"Objects/{path}");
		}
		
		public WearableData GetWearable(string path)
		{
			return (WearableData)GetData($"Wearables/{path}");
		}
		
		public CastData GetCast(string path)
		{
			return (CastData)GetData($"Casts/{path}");
		}
		
		public ProjectileData GetProjectile(string path)
		{
			return (ProjectileData)GetData($"Projectiles/{path}");
		}
		
		public AttackData GetAttack(string path)
		{
			return (AttackData)GetData($"Attacks/{path}");
		}
		
		public SpellData GetSpell(string path)
		{
			return (SpellData)GetData($"Spells/{path}");
		}
		
		public AliveData GetAlive(string path)
		{
			return (AliveData)GetData($"AI/{path}");
		}
		
		public DecalData GetDecal(string path)
		{
			return (DecalData)GetData($"Decals/{path}");
		}
		
		public SceneData GetScene(string path)
		{
			return (SceneData)GetData($"Scenes/{path}");
		}
		
		public PathData GetPath(string path)
		{
			return (PathData)GetData($"Paths/{path}");
		}
		
		#endregion

		#region Get All

		public SceneData[] GetAllScenes()
		{
			var list = new List<SceneData>();

			foreach (var pair in datasMap)
			{
				if (pair.Value is not SceneData sceneData)
					continue;

				list.Add(sceneData);
			}
			
			foreach (var pair in moddedDatasMap)
			{
				if (pair.Value is not SceneData sceneData)
					continue;

				list.Add(sceneData);
			}
			
			return list.ToArray();
		}

		#endregion

		#region Create

		public IObject CreateObject(ObjectData data, Vector3 position, Vector3 angles)
		{
			var obj = PoolingManager.Instance.TakeOrCreate<IObject>(data, false);
			obj.ObjectID = Guid.NewGuid().ToString();
			obj.ExternallySpawned = true;
			
			obj.Spawn(position, angles);
			
			return obj;
		}
		
		public IWearable CreateWearable(WearableData data, Vector3 position, Vector3 angles)
		{
			var wearable = PoolingManager.Instance.TakeOrCreate<IWearable>(data, false);
			wearable.ObjectID = Guid.NewGuid().ToString();
			
			wearable.Spawn(position, angles);
			
			return wearable;
		}
		
		public ICast CreateCast(CastData data, IIdentifiable source)
		{
			var cast = PoolingManager.Instance.TakeOrCreate<ICast>(data, false);
			cast.ObjectID = Guid.NewGuid().ToString();
			
			cast.Spawn(source);
			
			return cast;
		}
		
		public IProjectile CreateProjectile(ProjectileData data, float range, AttackData attack, IIdentifiable source, Vector3 origin, Vector3 direction, float elapsedTime = 0f)
		{
			var projectile = PoolingManager.Instance.TakeOrCreate<IProjectile>(data, false);
			projectile.ObjectID = Guid.NewGuid().ToString();
			projectile.ExternallySpawned = true;
			
			projectile.Spawn(source, range, attack, origin, direction * data.Force, elapsedTime);
			
			return projectile;
		}

		public IAttack CreateAttack(AttackData data, IIdentifiable source, RaycastHit hit, IIdentifiable attach, float elapsedTime = 0f)
		{
			return createAttack(data, source, hit.point, hit.normal, attach, elapsedTime);
		}

		public IAttack CreateAttack(AttackData data, IIdentifiable source, ContactPoint contact, IIdentifiable attach, float elapsedTime = 0f)
		{
			return createAttack(data, source, contact.point, contact.normal, attach, elapsedTime);
		}
		
		public IAttack CreateAttack(AttackData data, IIdentifiable source, Vector3 point, Vector3 normal, IIdentifiable attach, float elapsedTime = 0f)
		{
			return createAttack(data, source, point, normal, attach, elapsedTime);
		}
		
		public IDecal CreateDecal(DecalData data, ContactPoint contact, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f)
		{
			var decal = PoolingManager.Instance.TakeOrCreate<IDecal>(data, false);
			decal.ObjectID = Guid.NewGuid().ToString();
			decal.ExternallySpawned = true;
			
			decal.Spawn(contact.point, Quaternion.LookRotation(-contact.normal), attach, elapsedTime, normalizedTime);
			
			return decal;
		}
		
		public IDecal CreateDecal(DecalData data, ParticleCollisionEvent collisionEvent, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f)
		{
			return createDecal(data, collisionEvent.intersection, Quaternion.LookRotation(-collisionEvent.normal), attach, elapsedTime, normalizedTime);
		}
		
		public IDecal CreateDecal(DecalData data, Vector3 point, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f)
		{
			return createDecal(data, point, angles, attach, elapsedTime, normalizedTime);
		}

		private IDecal createDecal(DecalData data, Vector3 point, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f, float normalizedTime = 0f)
		{
			var decal = PoolingManager.Instance.TakeOrCreate<IDecal>(data, false);
			decal.ObjectID = Guid.NewGuid().ToString();
			decal.ExternallySpawned = true;
			
			decal.Spawn(point, angles, attach, elapsedTime, normalizedTime);
			
			return decal;
		}
		
		private IAttack createAttack(AttackData data, IIdentifiable source, Vector3 point, Vector3 normal, IIdentifiable attach, float elapsedTime)
		{
			var attack = PoolingManager.Instance.TakeOrCreate<IAttack>(data, false);
			attack.ObjectID = Guid.NewGuid().ToString();
			attack.ExternallySpawned = true;

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
							angles = source.GetTransform().rotation;
							break;
					}
					break;
				case EAttackAngle.Ray:
					angles = source is ISpell raySpell ? Quaternion.LookRotation(raySpell.LastRay.direction) : Quaternion.identity;
					break;
				default:
					throw new NotImplementedException();
			}

			switch (data.AttackOrigin)
			{
				case EAttackOrigin.Point:
					break;
				case EAttackOrigin.Origin:
					point = source is ISpell spell ? spell.LastRay.origin : point;
					break;
				default:
					throw new NotImplementedException();
			}
			
			attack.Spawn(source, point, angles, attach, elapsedTime);
			return attack;
		}
		
		#endregion
	}
}