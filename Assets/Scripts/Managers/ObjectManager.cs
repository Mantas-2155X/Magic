using System;
using System.Collections.Generic;
using System.IO;
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
				instance.setupModdedDatasMap();
				
				return instance;
			}
		}

		public static string ModsPath => "data/mods";
		
		private readonly Dictionary<string, Data> datasMap = new ();
		
		private readonly List<ModInfo> mods = new ();

		private readonly string[] dataPaths = { "Objects", "Wearables", "Casts", "Projectiles", "Attacks", "Spells", "AI", "Decals", "Scenes", "Paths" };

		private readonly List<Type> allowedModdedDatas = new ()
		{
			typeof(AttackData), 
			typeof(CastData), 
			typeof(DecalData), 
			typeof(ProjectileData), 
			typeof(SpellData), 
			typeof(WearableData)
		};
		
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

		private void setupModdedDatasMap()
		{
			if (!Directory.Exists(ModsPath))
				Directory.CreateDirectory(ModsPath);

			var platform = "";

			switch (Application.platform)
			{
				case RuntimePlatform.LinuxPlayer or RuntimePlatform.LinuxEditor:
					platform = "StandaloneLinux64";
					break;
				case RuntimePlatform.WindowsPlayer or RuntimePlatform.WindowsEditor:
					platform = "StandaloneWindows64";
					break;
			}

			if (platform == "")
			{
				Debug.LogError($"[ObjectManager] Modding on platform {Application.platform} is not supported");
				return;
			}
			
			var files = Directory.GetFiles(ModsPath, "info.tsv", SearchOption.AllDirectories);
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];
				var fileInfo = new FileInfo(file);
				
				var directory = fileInfo.Directory!.FullName;
				
				try
				{
					var lines = File.ReadAllLines(file);
					if (lines.Length < 5)
					{
						Debug.LogWarning($"[ObjectManager] Mod info at {directory} is incomplete, skipping");
						continue;
					}
					
					var authorSplit = lines[0].Split('\t');
					if (authorSplit.Length < 2 || authorSplit[0] != "Author" || string.IsNullOrWhiteSpace(authorSplit[1]))
					{
						Debug.LogWarning($"[ObjectManager] Author info at {directory} is incomplete, skipping");
						continue;
					}
					
					var author = authorSplit[1];

					var nameSplit = lines[1].Split('\t');
					if (nameSplit.Length < 2 || nameSplit[0] != "Name" || string.IsNullOrWhiteSpace(nameSplit[1]))
					{
						Debug.LogWarning($"[ObjectManager] Name info at {directory} is incomplete, skipping");
						continue;
					}
					
					var name = nameSplit[1];
					
					var versionSplit = lines[2].Split('\t');
					if (versionSplit.Length < 2 || versionSplit[0] != "Version" || string.IsNullOrWhiteSpace(versionSplit[1]))
					{
						Debug.LogWarning($"[ObjectManager] Version info at {directory} is incomplete, skipping");
						continue;
					}

					var version = versionSplit[1];
					
					if (!string.IsNullOrWhiteSpace(lines[3]))
					{
						Debug.LogWarning($"[ObjectManager] Separator at {directory} is invalid, skipping");
						continue;
					}
					
					var bundlePath = Path.Combine(directory, platform);
					var assetPath = Path.Combine(bundlePath, $"{author}.{name}-{version}".ToLower());
					
					if (!Directory.Exists(bundlePath) || !File.Exists(assetPath))
					{
						Debug.LogWarning($"[ObjectManager] Mod at {directory} does not have data for platform {platform}, skipping");
						continue;
					}

					var bundle = AssetBundle.LoadFromFile(assetPath);
					if (bundle == null)
					{
						Debug.LogWarning($"[ObjectManager] Failed to load bundle for mod at {directory}, skipping");
						continue;
					}

					var prefix = $"{author}.{name}.";
					var bundleDatas = bundle.LoadAllAssets<Data>();
					
					var datas = new Dictionary<string, Data>();
					for (var k = 4; k < lines.Length; k++)
					{
						var dataSplit = lines[k].Split('\t');
						if (dataSplit.Length != 2)
						{
							Debug.LogWarning($"[ObjectManager] Data info at line {k} for mod at {directory} is incomplete, skipping");
							continue;
						}

						var dataType = Type.GetType($"ScriptableObjects.{dataSplit[0]}");
						if (dataType == null)
						{
							Debug.LogWarning($"[ObjectManager] Data type at line {k} for mod at {directory} is invalid, skipping");
							continue;
						}
						
						if (!allowedModdedDatas.Contains(dataType))
						{
							Debug.LogWarning($"[ObjectManager] Data type at line {k} for mod at {directory} is not supported, skipping");
							continue;
						}

						var found = false;
						var dataName = dataSplit[1];

						for (var l = 0; l < bundleDatas.Length; l++)
						{
							var bundleData = bundleDatas[l];
							if (bundleData.Name != dataName || bundleData.GetType() != dataType)
								continue;
							
							bundleData.Name = prefix + bundleData.Name;
							bundleData.Description = prefix + bundleData.Description;
							
							found = true;
							datas[bundleData.Name] = bundleData;
							break;
						}

						if (!found)
						{
							Debug.LogWarning($"[ObjectManager] Mod bundle at {directory} does not contain object described at line {k}, skipping");
							continue;
						}
					}

					if (datas.Count == 0)
					{
						Debug.LogWarning($"[ObjectManager] Mod info at {directory} does not contain any datas, skipping");
						continue;
					}
					
					var mod = new ModInfo(author, name, version, bundle, datas);
					mods.Add(mod);
				}
				catch (Exception e)
				{
					Debug.LogError($"[ObjectManager] Exception loading mod at {directory}, {e}");
				}
			}
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
		
		public PathData GetPath(string path)
		{
			return (PathData)datasMap.GetValueOrDefault($"Paths/{path}");
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
			obj.ObjectID = Guid.NewGuid().ToString();
			
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
			
			decal.Spawn(point, angles, attach, elapsedTime, normalizedTime);
			
			return decal;
		}
		
		private IAttack createAttack(AttackData data, IIdentifiable source, Vector3 point, Vector3 normal, IIdentifiable attach, float elapsedTime)
		{
			var attack = PoolingManager.Instance.TakeOrCreate<IAttack>(data, false);
			attack.ObjectID = Guid.NewGuid().ToString();

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
		
		public class ModInfo
		{
			public string Author { get; }
			public string Name { get; }
			public string Version { get; }
			
			public AssetBundle Bundle { get; }
			
			public Dictionary<string, Data> Datas { get; }

			public ModInfo(string author, string name, string version, AssetBundle bundle, Dictionary<string, Data> datas)
			{
				Author = author;
				Name = name;
				Version = version;
				Bundle = bundle;
				Datas = datas;

				foreach (var pair in datas)
				{
					var typeName = pair.Value.GetType().Name;
					typeName = typeName[..^4];
					typeName += $"s/{pair.Key}";

					Instance.datasMap[typeName] = pair.Value;
				}
				
				Debug.Log($"[ObjectManager] Loaded mod {author}.{name}-{version} with {datas.Count} datas");
			}
		}
	}
}