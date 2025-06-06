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
using Newtonsoft.Json;
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
		
		public readonly List<Mod> Mods = new ();

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

		private readonly List<string> defaultSharedReferences = new ()
		{
			"mscorlib", 
			"netstandard", 
			"Assembly-CSharp"
		};

		private readonly List<string> whitelistedReferences = new()
		{
			"mscorlib", 
			"netstandard", 
			"Assembly-CSharp",
			"UnityEngine.CoreModule",
			"UnityEngine.PhysicsModule",
			"UnityEngine.AIModule",
			"Unity.InputSystem",
			"UnityEngine.ParticleSystemModule",
			"UniTask",
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

			if (string.IsNullOrWhiteSpace(platform))
			{
				Debug.LogError($"[ObjectManager] Modding on platform {Application.platform} is not supported");
				return;
			}
			
			var files = Directory.GetFiles(ModsPath, "info.json", SearchOption.AllDirectories);
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];
				var fileInfo = new FileInfo(file);
				
				var directory = fileInfo.Directory!.FullName;
				
				try
				{
					var text = File.ReadAllText(file);
					
					var modInfo = JsonConvert.DeserializeObject<ModInfo>(text);
					if (modInfo == null)
					{
						Debug.LogWarning($"[ObjectManager] Failed to load ModInfo for mod at {directory}, skipping");
						continue;
					}

					var validity = modInfo.Validate();
					if (validity != ModInfo.EModInfoValidity.Valid)
					{
						switch (validity)
						{
							case ModInfo.EModInfoValidity.InvalidAuthor:
								Debug.LogWarning($"[ObjectManager] Author is invalid for mod at {directory}, skipping");
								break;
							case ModInfo.EModInfoValidity.InvalidName:
								Debug.LogWarning($"[ObjectManager] Name is invalid for mod at {directory}, skipping");
								break;
							case ModInfo.EModInfoValidity.InvalidVersion:
								Debug.LogWarning($"[ObjectManager] Version is invalid for mod at {directory}, skipping");
								break;
							case ModInfo.EModInfoValidity.NoObjects:
								Debug.LogWarning($"[ObjectManager] No data objects for mod at {directory}, skipping");
								break;
						}
						
						continue;
					}
					
					var bundlePath = Path.Combine(directory, platform);
					var assetPath = Path.Combine(bundlePath, $"{modInfo.Author}.{modInfo.Name}".ToLower());
					
					if (!Directory.Exists(bundlePath) || !File.Exists(assetPath))
					{
						Debug.LogWarning($"[ObjectManager] Mod at {directory} does not have data for platform {platform}, skipping");
						continue;
					}

					var mod = new Mod(modInfo, directory, assetPath);
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
		
		[JsonObject]
		public class ModInfo
		{
			[JsonProperty]
			public string Author;
			
			[JsonProperty]
			public string Name;
			
			[JsonProperty]
			public string Version;

			[JsonProperty]
			public bool Disabled;
			
			[JsonProperty]
			public bool UseCustomAssembly;

			[JsonProperty]
			public List<ObjectInfo> Objects;
			
			public EModInfoValidity Validate()
			{
				if (string.IsNullOrWhiteSpace(Author))
					return EModInfoValidity.InvalidAuthor;

				if (string.IsNullOrWhiteSpace(Name))
					return EModInfoValidity.InvalidName;

				if (string.IsNullOrWhiteSpace(Version))
					return EModInfoValidity.InvalidVersion;

				if (Objects == null || Objects.Count == 0)
					return EModInfoValidity.NoObjects;

				return EModInfoValidity.Valid;
			}

			public enum EModInfoValidity
			{
				InvalidAuthor,
				InvalidName,
				InvalidVersion,
				NoObjects,
				Valid
			}
			
			[JsonObject]
			public class ObjectInfo
			{
				[JsonProperty]
				public string Type;
				
				[JsonProperty]
				public string Name;
			}
		}
		
		public class Mod
		{
			public ModInfo Info { get; private set; }
			
			public string Directory { get; private set; }
			
			public Tuple<string, AssetBundle> Bundle { get; private set; }

			public List<string> Addresses { get; private set; }
			
			public bool CustomAssemblyLoaded { get; private set; }

			public Mod(ModInfo info, string directory, string assetPath)
			{
				Info = info;
				Directory = directory;
				Bundle = new Tuple<string, AssetBundle>(assetPath, null);
				Addresses = new List<string>();
				CustomAssemblyLoaded = false;

				Debug.Log($"[ObjectManager] Preloaded mod {Info.Author}.{Info.Name} {Info.Version} ({(Info.Disabled ? "Disabled" : "Enabled")})");

				if (Info.Disabled)
					return;
				
				Load();
			}

			public void Load()
			{
				if (!CustomAssemblyLoaded && Info.UseCustomAssembly)
				{
					var assemblyPath = Path.Combine(Directory, $"{Info.Author}.{Info.Name}.dll");
					if (File.Exists(assemblyPath))
					{
						var assemblyBytes = File.ReadAllBytes(assemblyPath);

						var reflectionAssembly = Assembly.ReflectionOnlyLoad(assemblyBytes);
						if (reflectionAssembly == null)
						{
							Debug.LogWarning($"[ObjectManager] Failed to load custom assembly for mod at {Directory}, no content added");
							return;
						}

						var currentReferences = Assembly.GetExecutingAssembly().GetReferencedAssemblies();

						var referencedAssemblies = reflectionAssembly.GetReferencedAssemblies();
						for (var i = 0; i < referencedAssemblies.Length; i++)
						{
							var referencedAssembly = referencedAssemblies[i];
							var sharedReference = false;

							if (Instance.defaultSharedReferences.Contains(referencedAssembly.Name))
							{
								sharedReference = true;
							}
							else
							{
								for (var k = 0; k < currentReferences.Length; k++)
								{
									var currentReference = currentReferences[k];
									if (currentReference.FullName != referencedAssembly.FullName)
										continue;

									sharedReference = true;
									break;
								}
							}

							if (!sharedReference)
							{
								Debug.LogWarning($"[ObjectManager] Custom assembly references non-shared reference {referencedAssembly.FullName} for mod at {Directory}, no content added");
								return;
							}

							if (!Instance.whitelistedReferences.Contains(referencedAssembly.Name))
							{
								Debug.LogWarning($"[ObjectManager] Custom assembly references non-whitelisted reference {referencedAssembly.FullName} for mod at {Directory}, no content added");
								return;
							}
						}
						
						var assemblyName = reflectionAssembly.GetName().Name;
						if (assemblyName != $"{Info.Author}.{Info.Name}")
						{
							Debug.LogWarning($"[ObjectManager] Invalid custom assembly name (should be {Info.Author}.{Info.Name}, is {assemblyName}) for mod at {Directory}, no content added");
							return;
						}
					
						CustomAssemblyLoaded = true;
						Assembly.Load(assemblyBytes);
					}
					else
					{
						Debug.LogWarning($"[ObjectManager] Could not find custom assembly for mod at {Directory}, no content added");
						return;
					}
				}
				
				var bundle = AssetBundle.LoadFromFile(Bundle.Item1);
				if (bundle == null)
				{
					Debug.LogWarning($"[ObjectManager] Failed to load bundle for mod at {Directory}, no content added");
					return;
				}

				Bundle = new Tuple<string, AssetBundle>(Bundle.Item1, bundle);
				
				var prefix = $"{Info.Author}.{Info.Name}.";
				var bundleDatas = bundle.LoadAllAssets<Data>();

				for (var i = 0; i < Info.Objects.Count; i++)
				{
					var obj = Info.Objects[i];
					
					if (string.IsNullOrEmpty(obj.Name) || string.IsNullOrEmpty(obj.Type))
					{
						Debug.LogWarning($"[ObjectManager] Data type at line {i} for mod at {Directory} is invalid, skipping object");
						continue;
					}

					var dataType = Type.GetType($"ScriptableObjects.{obj.Type}");
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

					for (var k = 0; k < bundleDatas.Length; k++)
					{
						var bundleData = bundleDatas[k];
						if (bundleData.Name != obj.Name || bundleData.GetType() != dataType)
							continue;
							
						bundleData.Name = prefix + bundleData.Name;
						bundleData.Description = prefix + bundleData.Description;

						if (!string.IsNullOrWhiteSpace(bundleData.Type))
							bundleData.Assembly = $"{Info.Author}.{Info.Name}";

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

				Debug.Log($"[ObjectManager] Loaded mod {Info.Author}.{Info.Name} {Info.Version} with {Addresses.Count} datas {(CustomAssemblyLoaded ? "and custom assembly" : "")}");
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
				Debug.Log($"[ObjectManager] Unloaded mod {Info.Author}.{Info.Name} {Info.Version}");
			}

			public void Enable()
			{
				if (!Info.Disabled)
					return;
				
				Load();
				
				Info.Disabled = false;
				File.WriteAllText(Path.Combine(Directory, "info.json"), JsonConvert.SerializeObject(Info));
			}

			public void Disable()
			{
				if (Info.Disabled)
					return;
				
				if (Bundle.Item2 != null)
					Unload();

				Info.Disabled = true;
				File.WriteAllText(Path.Combine(Directory, "info.json"), JsonConvert.SerializeObject(Info));
			}
		}
	}
}