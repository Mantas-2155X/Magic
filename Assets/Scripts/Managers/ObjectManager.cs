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
using Mono.Cecil;
using Newtonsoft.Json;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

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
			//typeof(AliveData),
			typeof(AttackData), 
			typeof(CastData), 
			//typeof(Data), 
			typeof(DecalData), 
			//typeof(NPCData), 
			typeof(ObjectData), 
			typeof(PathData), 
			//typeof(PlayerData), 
			typeof(ProjectileData), 
			typeof(SceneData), 
			typeof(SpellData), 
			typeof(WearableData)
		};

		private readonly List<string> whitelistedReferences = new ()
		{
			"mscorlib", 
			"Assembly-CSharp",
			"UnityEngine.CoreModule",
			"UnityEngine.PhysicsModule",
			"UnityEngine.AIModule",
			"Unity.InputSystem",
			"UnityEngine.ParticleSystemModule",
			"UniTask",
			"Newtonsoft.Json"
		};

		private readonly List<string> blacklistedNamespaces = new ()
		{
			"Microsoft.CSharp",
			"Microsoft.VisualBasic",
			"Microsoft.Win32",
			"Microsoft.Win32.SafeHandles",
			"Mono.Net.Security",
			"System.CodeDom",
			"System.CodeDom.Compiler",
			"System.Diagnostics",
			"System.IO",
			"System.IO.Enumeration",
			"System.IO.IsolatedStorage",
			"System.IO.Compression",
			"System.IO.Ports",
			"System.Net",
			"System.Net.Cache",
			"System.Net.Configuration",
			"System.Net.Mail",
			"System.Net.Mime",
			"System.Net.NetworkInformation",
			"System.Net.Security",
			"System.Net.Sockets",
			"System.Net.WebSockets",
			"System.Net.Reflection",
			"System.Web",
			"System.Dynamic",
			"System.IO.MemoryMappedFiles",
			"System.IO.Pipes",
			"System.Runtime.CompilerServices",
			"System.Runtime.InteropServices",
			"System.Reflection",
			"System.Reflection.Emit",
		};

		private readonly Dictionary<string, List<string>> blacklistedTypes = new()
		{
			{"System", new List<string>
			{
				"ActivationContext", 
				"Activator", 
				"AppDomain", 
				"AppDomainInitializer", 
				"AppDomainManager", 
				"AppDomainSetup",
				"Environment",
				"GC",
				"MemoryExtensions"
			}},
			{"Managers", new List<string>
			{
				"ConsoleManager",
				"LocalizationManager",
				"SceneManager",
				"StateManager"
			}}
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

			var foundMods = new Dictionary<ModInfo, Tuple<string, string>>();
			
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
					var catalogPath = Path.Combine(bundlePath, $"{modInfo.Author}.{modInfo.Name}.bin");
					
					if (!Directory.Exists(bundlePath) || !File.Exists(catalogPath))
					{
						Debug.LogWarning($"[ObjectManager] Mod at {directory} does not have data for platform {platform}, skipping");
						continue;
					}

					foundMods.Add(modInfo, new Tuple<string, string>(directory, catalogPath));
					Debug.Log($"[ObjectManager] Preloaded mod {modInfo.Author}.{modInfo.Name} {modInfo.Version} ({(modInfo.Disabled ? "Disabled" : "Enabled")})");
				}
				catch (Exception e)
				{
					Debug.LogError($"[ObjectManager] Exception preloading mod at {directory}, {e}");
				}
			}

			var excludeMods = new List<ModInfo>();

			foreach (var (info, _) in foundMods)
			{
				var guid = info.GetGUID();

				foreach (var (innerInfo, _) in foundMods)
				{
					if (info == innerInfo)
						continue;
					
					var innerGuid = innerInfo.GetGUID();
					if (innerGuid != guid)
						continue;

					excludeMods.AddUnique(innerInfo);
					break;
				}
			}

			foreach (var (info, (directory, assetPath)) in foundMods)
			{
				if (excludeMods.Contains(info))
				{
					Debug.LogWarning($"[ObjectManager] Skipping loading mod {info.GetGUID()} {info.Version} at {directory} because multiple instances of it are installed");
					continue;
				}

				try
				{
					var mod = new Mod(info, directory, assetPath);
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
			
			[JsonProperty]
			public List<LocalizationInfo> Localizations;
			
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

			public string GetGUID()
			{
				return $"{Author}.{Name}";
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

			[JsonObject]
			public class LocalizationInfo
			{
				[JsonProperty]
				public string Language;

				[JsonProperty]
				public Dictionary<string, string> Entries;
			}
		}
		
		public class Mod
		{
			public ModInfo Info { get; private set; }
			
			public string Directory { get; private set; }
			
			public Tuple<string, IResourceLocator> Catalog { get; private set; }

			public List<string> Addresses { get; private set; }
			
			public bool CustomAssemblyLoaded { get; private set; }

			public Mod(ModInfo info, string directory, string catalogPath)
			{
				Info = info;
				Directory = directory;
				Catalog = new Tuple<string, IResourceLocator>(catalogPath, null);
				Addresses = new List<string>();
				CustomAssemblyLoaded = false;

				if (Info.Disabled)
					return;
				
				Load();
			}

			public void Load()
			{
				if (!CustomAssemblyLoaded && Info.UseCustomAssembly)
				{
					var assemblyPath = Path.Combine(Directory, $"{Info.Author}.{Info.Name}.dll");
					if (!File.Exists(assemblyPath))
					{
						Debug.LogWarning($"[ObjectManager] Could not find custom assembly for mod at {Directory}, no content added");
						return;
					}
					
					AssemblyDefinition assemblyDefinition;

					try
					{
						assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
					}
					catch (Exception e)
					{
						Debug.LogWarning($"[ObjectManager] Failed to read custom assembly definition for mod at {Directory}, no content added, {e}");
						return;
					}

					var assemblyName = assemblyDefinition.Name.Name;
					if (assemblyName != $"{Info.Author}.{Info.Name}")
					{
						Debug.LogWarning($"[ObjectManager] Invalid custom assembly name (should be {Info.Author}.{Info.Name}, is {assemblyName}) for mod at {Directory}, no content added");
						return;
					}

					var references = assemblyDefinition.MainModule.AssemblyReferences;
					for (var i = 0; i < references.Count; i++)
					{
						var reference = references[i];

						if (Instance.whitelistedReferences.Contains(reference.Name))
							continue;

						Debug.LogWarning($"[ObjectManager] Custom assembly references non-whitelisted reference {reference.FullName} for mod at {Directory}, no content added");
						return;
					}

					var currentReferences = AssemblyDefinition.ReadAssembly(typeof(ObjectManager).Assembly.Location).MainModule.AssemblyReferences;

					for (var i = references.Count - 1; i >= 0; i--)
					{
						var reference = references[i];

						for (var k = 0; k < currentReferences.Count; k++)
						{
							var currentReference = currentReferences[k];

							if (reference.Name != currentReference.Name)
								continue;

							references[i] = currentReference;
						}
					}

					var types = assemblyDefinition.MainModule.Types;
					for (var i = 0; i < types.Count; i++)
					{
						var typeDefinition = types[i];

						var methods = typeDefinition.Methods;
						for (var k = 0; k < methods.Count; k++)
						{
							var methodDefinition = methods[k];
							if (!methodDefinition.HasBody)
								continue;

							var instructions = methodDefinition.Body.Instructions;
							for (var j = 0; j < instructions.Count; j++)
							{
								var instruction = instructions[j];

								var operand = instruction.Operand;
								if (operand is not MethodReference reference)
									continue;

								var referenceNamespace = reference.DeclaringType.Namespace;

								if (Instance.blacklistedNamespaces.Contains(referenceNamespace))
								{
									Debug.LogWarning($"[ObjectManager] Custom assembly uses blacklisted namespace {referenceNamespace} for mod at {Directory}, no content added");
									return;
								}

								var referenceType = reference.DeclaringType.Name;

								if (Instance.blacklistedTypes.TryGetValue(referenceNamespace, out var typesList) && typesList.Contains(referenceType))
								{
									Debug.LogWarning($"[ObjectManager] Custom assembly uses blacklisted type {referenceNamespace}.{referenceType} for mod at {Directory}, no content added");
									return;
								}
							}
						}
					}

					using var stream = new MemoryStream();
					assemblyDefinition.Write(stream);
					stream.Position = 0;
					
					var bytes = stream.ToArray();
					
					CustomAssemblyLoaded = true;
					Assembly.Load(bytes);
				}
				
				var previousTransformFunction = Addressables.InternalIdTransformFunc;

				var prefix = $"{Info.Author}.{Info.Name}.";
				var bundleDatas = new List<Data>();

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
				
				try
				{
					Addressables.InternalIdTransformFunc = location => !location.InternalId.StartsWith(platform) ? location.InternalId : $"{Directory}/{location.InternalId}";
					
					var locator = Addressables.LoadContentCatalogAsync(Catalog.Item1).WaitForCompletion();
					if (locator == null)
					{
						Addressables.InternalIdTransformFunc = previousTransformFunction;
						
						Debug.LogWarning($"[ObjectManager] Failed to load bundle for mod at {Directory}, no content added");
						return;
					}

					Catalog = new Tuple<string, IResourceLocator>(Catalog.Item1, locator);
					
					foreach (var key in locator.Keys)
					{
						if (!locator.Locate(key, typeof(Data), out var locations))
							continue;

						for (var i = 0; i < locations.Count; i++)
						{
							var location = locations[i];
						
							if (!location.PrimaryKey.EndsWith(".asset"))
								continue;
						
							bundleDatas.Add(Addressables.LoadAssetAsync<Data>(location).WaitForCompletion());
						}
					}
				}
				catch (Exception e)
				{
					Addressables.InternalIdTransformFunc = previousTransformFunction;

					Debug.Log($"[ObjectManager] Failed grabbing locator datas for mod at {Directory}, no content added, {e}");
					return;
				}
				
				Addressables.InternalIdTransformFunc = previousTransformFunction;

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

					for (var k = 0; k < bundleDatas.Count; k++)
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

				for (var i = 0; i < Info.Localizations.Count; i++)
				{
					var localization = Info.Localizations[i];

					try
					{
						foreach (var pair in localization.Entries)
						{
							var key = $"{prefix}{pair.Key}";

							if (LocalizationManager.Instance.AddLocalizedEntry($"{prefix}{pair.Key}", pair.Value, localization.Language))
								continue;

							Debug.LogWarning($"[ObjectManager] Failed to add localization entry for language {localization.Language} with key {key} and value {pair.Value} for mod at {Directory}");
						}
					}
					catch (Exception e)
					{
						Debug.LogWarning($"[ObjectManager] Failed to add {localization.Language} localization entries for mod at at {Directory}, {e}");
					}
				}
				
				if (Addresses.Count == 0)
				{
					Debug.LogWarning($"[ObjectManager] Mod info at {Directory} does not contain any objects, no content added");
					return;
				}

				Debug.Log($"[ObjectManager] Loaded mod {Info.Author}.{Info.Name} {Info.Version} with {Addresses.Count} object(s) {(CustomAssemblyLoaded ? "and custom assembly" : "")}");
			}
				
			public void Unload()
			{
				for (var i = 0; i < Addresses.Count; i++)
					Instance.datasMap.Remove(Addresses[i]);

				Addresses.Clear();
				
				Catalog = new Tuple<string, IResourceLocator>(Catalog.Item1, null);
				Debug.Log($"[ObjectManager] Unloaded mod {Info.Author}.{Info.Name} {Info.Version}");
			}

			public void Enable()
			{
				if (!Info.Disabled)
					return;
				
				Load();
				
				Info.Disabled = false;
				File.WriteAllText(Path.Combine(Directory, "info.json"), JsonConvert.SerializeObject(Info, Formatting.Indented));
			}

			public void Disable()
			{
				if (Info.Disabled)
					return;
				
				if (Catalog.Item2 != null)
					Unload();

				Info.Disabled = true;
				File.WriteAllText(Path.Combine(Directory, "info.json"), JsonConvert.SerializeObject(Info, Formatting.Indented));
			}
		}
	}
}