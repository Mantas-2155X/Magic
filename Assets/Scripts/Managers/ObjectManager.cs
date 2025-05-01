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
using Objects.Interfaces;
using ScriptableObjects;
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
				
				return instance;
			}
		}

		private readonly Dictionary<string, Data> datasMap = new ();
		private readonly List<IObject> activeObjects = new ();

		private readonly string[] dataPaths = { "Objects", "Wearables", "Casts", "Projectiles", "Attacks", "Spells", "AI", "Decals", "Scenes" };

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
		
		public DecalData GetDecal(string path)
		{
			return (DecalData)datasMap.GetValueOrDefault($"Decals/{path}");
		}
		
		public SceneData GetScene(string path)
		{
			return (SceneData)datasMap.GetValueOrDefault($"Scenes/{path}");
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
			
			return list.ToArray();
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
		
		public IAttack CreateAttack(AttackData data, Component source, Vector3 point, Vector3 normal, Transform attach)
		{
			return createAttack(data, source, point, normal, attach);
		}
		
		public IDecal CreateDecal(DecalData data, ContactPoint contact, Transform attach)
		{
			var decal = PoolingManager.Instance.TakeOrCreate<IDecal>(data, false);
			decal.Spawn(contact.point, Quaternion.LookRotation(-contact.normal), attach);
			
			return decal;
		}
		
		public IDecal CreateDecal(DecalData data, ParticleCollisionEvent collisionEvent, Transform attach)
		{
			return createDecal(data, collisionEvent.intersection, Quaternion.LookRotation(-collisionEvent.normal), attach);
		}
		
		public IDecal CreateDecal(DecalData data, Vector3 point, Quaternion angles, Transform attach)
		{
			return createDecal(data, point, angles, attach);
		}

		private IDecal createDecal(DecalData data, Vector3 point, Quaternion angles, Transform attach)
		{
			var decal = PoolingManager.Instance.TakeOrCreate<IDecal>(data, false);
			decal.Spawn(point, angles, attach);
			
			return decal;
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
			
			attack.Spawn(source, point, angles, attach);
			return attack;
		}
		
		#endregion
	}
}