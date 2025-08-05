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
#if PRINT_DATAS
				instance.printDatasMap();
#endif
				
				return instance;
			}
		}

		private readonly Dictionary<string, Data> datasMap = new ();
		
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

			foreach (var pair in ModLoader.Instance.GetModdedDatas())
				Debug.Log($"[ContentManager] {pair.Value.GetType().Name} - {pair.Key}");
		}
		
		#endregion

		#region Get

		public T GetData<T>(string path, bool appendType = true) where T : Data
		{
			if (appendType)
			{
				string append;
				
				var type = typeof(T);
				if (type == typeof(AliveData))
				{
					append = "AI/";
				}
				else
				{
					var typeName = type.Name;
					
					var dataIndex = typeName.LastIndexOf("Data", StringComparison.Ordinal);
					if (dataIndex == -1)
					{
						Debug.LogError($"[ObjectManager] Failed to create data path for {typeof(T)} as the data index was not found");
						return null;
					}
					
					append = typeName[..dataIndex] + "s/";
				}
				
				path = append + path;
			}
			
			if (datasMap.TryGetValue(path, out var data) && data is T castedData)
				return castedData;

			var moddedDatas = ModLoader.Instance.GetModdedDatas();
			if (moddedDatas.TryGetValue(path, out var moddedData) && moddedData is T castedModdedData)
				return castedModdedData;

			return null;
		}
		
		#endregion

		#region Get All

		public List<T> GetAllDatas<T>() where T : Data
		{
			var list = new List<T>();

			foreach (var pair in datasMap)
			{
				if (pair.Value is not T data)
					continue;

				list.Add(data);
			}

			var moddedDatasMap = ModLoader.Instance.GetModdedDatas();
			foreach (var pair in moddedDatasMap)
			{
				if (pair.Value is not T data)
					continue;

				list.Add(data);
			}
			
			return list;
		}

		#endregion

		#region Create

		public IObject CreateObject(ObjectData data, Vector3 position, Vector3 angles)
		{
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to CreateObject is null");
				return null;
			}
			
			var obj = PoolingManager.Instance.TakeOrCreate<IObject>(data, false);
			obj.ObjectID = Guid.NewGuid().ToString();
			obj.ExternallySpawned = true;
			
			obj.Spawn(position, angles);
			
			return obj;
		}
		
		public IWearable CreateWearable(WearableData data, Vector3 position, Vector3 angles)
		{
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to CreateWearable is null");
				return null;
			}
			
			var wearable = PoolingManager.Instance.TakeOrCreate<IWearable>(data, false);
			wearable.ObjectID = Guid.NewGuid().ToString();
			
			wearable.Spawn(position, angles);
			
			return wearable;
		}
		
		public ICast CreateCast(CastData data, IIdentifiable source)
		{
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to CreateCast is null");
				return null;
			}
			
			var cast = PoolingManager.Instance.TakeOrCreate<ICast>(data, false);
			cast.ObjectID = Guid.NewGuid().ToString();
			
			cast.Spawn(source);
			
			return cast;
		}
		
		public IProjectile CreateProjectile(ProjectileData data, float range, AttackData attack, IIdentifiable source, Vector3 origin, Vector3 direction, float elapsedTime = 0f)
		{
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to CreateProjectile is null");
				return null;
			}
			
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
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to CreateDecal is null");
				return null;
			}
			
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
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to createDecal is null");
				return null;
			}
			
			var decal = PoolingManager.Instance.TakeOrCreate<IDecal>(data, false);
			decal.ObjectID = Guid.NewGuid().ToString();
			decal.ExternallySpawned = true;
			
			decal.Spawn(point, angles, attach, elapsedTime, normalizedTime);
			
			return decal;
		}
		
		private IAttack createAttack(AttackData data, IIdentifiable source, Vector3 point, Vector3 normal, IIdentifiable attach, float elapsedTime)
		{
			if (data == null)
			{
				Debug.LogError("[ObjectManager] Data provided to createAttack is null");
				return null;
			}
			
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