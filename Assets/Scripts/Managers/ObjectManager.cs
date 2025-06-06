//#define PRINT_DATAS

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
#if PRINT_DATAS
				instance.printDatasMap();
#endif
				
				return instance;
			}
		}

		public static string ModsPath => "data/mods";
		
		public readonly List<ModInfo> Mods = new ();

		private readonly Dictionary<string, Data> datasMap = new ();
		
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
					var disabled = lines[3] == "Disabled";
					
					if (!string.IsNullOrWhiteSpace(lines[3]) && !disabled)
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

					var mod = new ModInfo(author, name, version, directory, disabled, assetPath, lines);
					Mods.Add(mod);
				}
				catch (Exception e)
				{
					Debug.LogError($"[ObjectManager] Exception loading mod at {directory}, {e}");
				}
			}
		}

		private void printDatasMap()
		{
			Debug.Log("[ContentManager] Printing datas map");

			foreach (var pair in datasMap)
				Debug.Log($"[ContentManager] {pair.Value.GetType().Name} - {pair.Key}");
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
			public string Author { get; private set; }
			public string Name { get; private set; }
			public string Version { get; private set; }
			
			public string Directory { get; private set; }
			public bool Disabled { get; private set; }

			public string[] Lines { get; private set; }
			
			public Tuple<string, AssetBundle> Bundle { get; private set; }

			public List<string> Addresses { get; private set; }
			
			public bool CustomAssembly { get; private set; }

			public ModInfo(string author, string name, string version, string directory, bool disabled, string bundlePath, string[] lines)
			{
				Author = author;
				Name = name;
				Version = version;
				Directory = directory;
				Disabled = disabled;
				Lines = lines;
				Bundle = new Tuple<string, AssetBundle>(bundlePath, null);
				Addresses = new List<string>();
				CustomAssembly = false;

				Debug.Log($"[ObjectManager] Preloaded mod {Author}.{Name}-{Version} ({(disabled ? "Disabled" : "Enabled")})");

				if (disabled)
					return;
				
				Load();
			}

			public void Load()
			{
				if (!CustomAssembly)
				{
					var assemblyPath = Path.Combine(Directory, $"{Author}.{Name}.dll");
					if (File.Exists(assemblyPath))
					{
						var assemblyBytes = File.ReadAllBytes(assemblyPath);

						var reflectionAssembly = Assembly.ReflectionOnlyLoad(assemblyBytes);
						if (reflectionAssembly == null)
						{
							Debug.LogWarning($"[ObjectManager] Failed to load custom assembly for mod at {Directory}, no content added");
							return;
						}

						var assemblyName = reflectionAssembly.GetName().Name;
						if (assemblyName != $"{Author}.{Name}")
						{
							Debug.LogWarning($"[ObjectManager] Invalid custom assembly name (should be {Author}.{Name}, is {assemblyName}) for mod at {Directory}, no content added");
							return;
						}
					
						CustomAssembly = true;
						Assembly.Load(assemblyBytes);
					}
				}
				
				var bundle = AssetBundle.LoadFromFile(Bundle.Item1);
				if (bundle == null)
				{
					Debug.LogWarning($"[ObjectManager] Failed to load bundle for mod at {Directory}, no content added");
					return;
				}

				Bundle = new Tuple<string, AssetBundle>(Bundle.Item1, bundle);
				
				var prefix = $"{Author}.{Name}.";
				var bundleDatas = bundle.LoadAllAssets<Data>();

				for (var i = 4; i < Lines.Length; i++)
				{
					var dataSplit = Lines[i].Split('\t');
					if (dataSplit.Length != 2)
					{
						Debug.LogWarning($"[ObjectManager] Data info at line {i} for mod at {Directory} is incomplete, skipping object");
						continue;
					}

					var dataType = Type.GetType($"ScriptableObjects.{dataSplit[0]}");
					if (dataType == null)
					{
						Debug.LogWarning($"[ObjectManager] Data type at line {i} for mod at {Directory} is invalid, skipping object");
						continue;
					}
						
					if (!Instance.allowedModdedDatas.Contains(dataType))
					{
						Debug.LogWarning($"[ObjectManager] Data type at line {i} for mod at {Directory} is not supported, skipping object");
						continue;
					}

					var found = false;
					var dataName = dataSplit[1];

					for (var k = 0; k < bundleDatas.Length; k++)
					{
						var bundleData = bundleDatas[k];
						if (bundleData.Name != dataName || bundleData.GetType() != dataType)
							continue;
							
						bundleData.Name = prefix + bundleData.Name;
						bundleData.Description = prefix + bundleData.Description;

						if (bundleData.Type != "")
							bundleData.Assembly = $"{Author}.{Name}";

						var address = dataType.Name[..^4] + $"s/{bundleData.Name}";
						Addresses.Add(address);
						
						Instance.datasMap[address] = bundleData;
						
						found = true;
						break;
					}

					if (!found)
					{
						Debug.LogWarning($"[ObjectManager] Mod bundle at {Directory} does not contain object described at line {i}, skipping object");
						continue;
					}
				}

				if (Addresses.Count == 0)
				{
					Debug.LogWarning($"[ObjectManager] Mod info at {Directory} does not contain any datas, no content added");
					return;
				}

				Debug.Log($"[ObjectManager] Loaded mod {Author}.{Name}-{Version} with {Addresses.Count} datas {(CustomAssembly ? "and custom assembly" : "")}");
			}
				
			public void Unload()
			{
				for (var i = 0; i < Addresses.Count; i++)
					Instance.datasMap.Remove(Addresses[i]);

				Addresses.Clear();
				
				var assetBundle = Bundle.Item2;
				if (assetBundle != null)
					assetBundle.Unload(true);
				
				Bundle = new Tuple<string, AssetBundle>(Bundle.Item1, null);
				Debug.Log($"[ObjectManager] Unloaded mod {Author}.{Name}-{Version}");
			}

			public void Enable()
			{
				if (!Disabled)
					return;
				
				Load();
				
				Disabled = false;

				var infoPath = Path.Combine(Directory, "info.tsv");
				
				var lines = File.ReadAllLines(infoPath);
				lines[3] = "";
				
				File.WriteAllLines(infoPath, lines);
			}

			public void Disable()
			{
				if (Disabled)
					return;
				
				if (Bundle.Item2 != null)
					Unload();

				Disabled = true;
				
				var infoPath = Path.Combine(Directory, "info.tsv");
				
				var lines = File.ReadAllLines(infoPath);
				lines[3] = "Disabled";
				
				File.WriteAllLines(infoPath, lines);
			}
		}
	}
}