using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Managers;
using Modding.Enums;
using Modding.Infos;
using Mono.Cecil;
using Newtonsoft.Json;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Modding
{
	public class ModLoader
	{
		private static ModLoader instance;
		public static ModLoader Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new ModLoader();
				instance.loadMods();
				
				return instance;
			}
		}
		
		public static string ModsPath => "data/mods";
		
		private readonly List<Mod> mods = new ();
		
		private readonly Dictionary<string, Data> moddedDatasMap = new ();

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
			"Modding"
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
		
		#region Get

		public List<ModInfo> GetModInfos()
		{
			var list = new List<ModInfo>();
			
			for (var i = 0; i < mods.Count; i++)
				list.Add(mods[i].Info);

			return list;
		}

		public Dictionary<string, Data> GetModdedDatas()
		{
			return moddedDatasMap;
		}
		
		public List<string> GetAddresses(ModInfo modInfo)
		{
			if (modInfo == null)
				return null;
			
			var mod = getMod(modInfo);
			if (mod == null)
				return null;

			return mod.Addresses;
		}

		public string GetDirectory(ModInfo modInfo)
		{
			if (modInfo == null)
				return null;
			
			var mod = getMod(modInfo);
			if (mod == null)
				return null;

			return mod.Directory;
		}
		
		#endregion

		#region API

		public bool EnableMod(ModInfo modInfo)
		{
			if (modInfo == null || !modInfo.Disabled)
				return false;
			
			var mod = getMod(modInfo);
			if (mod == null)
				return false;

			loadMod(mod);
			
			modInfo.Disabled = false;
			File.WriteAllText(Path.Combine(mod.Directory, "info.json"), JsonConvert.SerializeObject(modInfo, Formatting.Indented));
			
			return true;
		}

		public bool DisableMod(ModInfo modInfo)
		{
			if (modInfo == null || modInfo.Disabled)
				return false;

			var mod = getMod(modInfo);
			if (mod == null)
				return false;
			
			if (mod.Catalog.Item2 != null)
				unloadMod(mod);

			modInfo.Disabled = true;
			File.WriteAllText(Path.Combine(mod.Directory, "info.json"), JsonConvert.SerializeObject(modInfo, Formatting.Indented));

			return true;
		}
		
		#endregion

		#region Internals

		private Mod getMod(ModInfo modInfo)
		{
			for (var i = 0; i < mods.Count; i++)
			{
				var mod = mods[i];
				if (mod.Info != modInfo)
					continue;

				return mod;
			}

			return null;
		}

		private bool loadMod(Mod mod)
		{
			var info = mod.Info;
			
			if (!mod.CustomAssemblyLoaded && info.UseCustomAssembly)
			{
				if (!loadCustomAssembly(mod))
					return false;
			}
			
			var previousTransformFunction = Addressables.InternalIdTransformFunc;

			var prefix = $"{info.GetGUID()}.";
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
				Addressables.InternalIdTransformFunc = location => !location.InternalId.StartsWith(platform) ? location.InternalId : $"{mod.Directory}/{location.InternalId}";
				
				var locator = Addressables.LoadContentCatalogAsync(mod.Catalog.Item1).WaitForCompletion();
				if (locator == null)
				{
					Addressables.InternalIdTransformFunc = previousTransformFunction;
					
					Debug.LogWarning($"[ModLoader] Failed to load bundle for mod at {mod.Directory}, no content added");
					return false;
				}

				mod.Catalog = new Tuple<string, IResourceLocator>(mod.Catalog.Item1, locator);
				
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

				Debug.Log($"[ModLoader] Failed grabbing locator datas for mod at {mod.Directory}, no content added, {e}");
				return false;
			}
			
			Addressables.InternalIdTransformFunc = previousTransformFunction;

			for (var i = 0; i < info.Objects.Count; i++)
			{
				var obj = info.Objects[i];
				
				if (string.IsNullOrEmpty(obj.Name) || string.IsNullOrEmpty(obj.Type))
				{
					Debug.LogWarning($"[ModLoader] Data type at line {i} for mod at {mod.Directory} is invalid, skipping object");
					continue;
				}

				var dataType = Type.GetType($"ScriptableObjects.{obj.Type}");
				if (dataType == null)
				{
					Debug.LogWarning($"[ModLoader] Data type at line {i} for mod at {mod.Directory} is invalid, skipping object");
					continue;
				}
					
				if (!isAllowedModdedData(dataType))
				{
					Debug.LogWarning($"[ModLoader] Data type at line {i} for mod at {mod.Directory} is not supported, skipping object");
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
						bundleData.Assembly = info.GetGUID();

					var address = dataType.Name[..^4] + $"s/{bundleData.Name}";
					mod.Addresses.Add(address);

					moddedDatasMap[address] = bundleData;
					
					found = true;
					break;
				}

				if (!found)
				{
					Debug.LogWarning($"[ModLoader] Mod bundle at {mod.Directory} does not contain object described at line {i}, skipping object");
				}
			}

			for (var i = 0; i < info.Localizations.Count; i++)
			{
				var localization = info.Localizations[i];

				try
				{
					foreach (var pair in localization.Entries)
					{
						var key = $"{prefix}{pair.Key}";

						if (LocalizationManager.Instance.AddLocalizedEntry(key, pair.Value, localization.Language))
							continue;

						Debug.LogWarning($"[ModLoader] Failed to add localization entry for language {localization.Language} with key {key} and value {pair.Value} for mod at {mod.Directory}");
					}
				}
				catch (Exception e)
				{
					Debug.LogWarning($"[ModLoader] Failed to add {localization.Language} localization entries for mod at at {mod.Directory}, {e}");
				}
			}
			
			if (mod.Addresses.Count == 0)
			{
				Debug.LogWarning($"[ModLoader] Mod info at {mod.Directory} does not contain any objects, no content added");
				return false;
			}

			Debug.Log($"[ModLoader] Loaded mod {info.GetGUID()} {info.Version} with {mod.Addresses.Count} object(s) {(mod.CustomAssemblyLoaded ? "and custom assembly" : "")}");
			return true;
		}

		private bool unloadMod(Mod mod)
		{
			for (var i = 0; i < mod.Addresses.Count; i++)
				moddedDatasMap.Remove(mod.Addresses[i]);

			mod.Addresses.Clear();
			
			mod.Catalog = new Tuple<string, IResourceLocator>(mod.Catalog.Item1, null);
			Debug.Log($"[ModLoader] Unloaded mod {mod.Info.GetGUID()} {mod.Info.Version}");

			return true;
		}

		private void loadMods()
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
				Debug.LogError($"[ModLoader] Modding on platform {Application.platform} is not supported");
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
						Debug.LogWarning($"[ModLoader] Failed to load ModInfo for mod at {directory}, skipping");
						continue;
					}

					var validity = modInfo.Validate();
					if (validity != EModInfoValidity.Valid)
					{
						switch (validity)
						{
							case EModInfoValidity.InvalidAuthor:
								Debug.LogWarning($"[ModLoader] Author is invalid for mod at {directory}, skipping");
								break;
							case EModInfoValidity.InvalidName:
								Debug.LogWarning($"[ModLoader] Name is invalid for mod at {directory}, skipping");
								break;
							case EModInfoValidity.InvalidVersion:
								Debug.LogWarning($"[ModLoader] Version is invalid for mod at {directory}, skipping");
								break;
							case EModInfoValidity.NoObjects:
								Debug.LogWarning($"[ModLoader] No data objects for mod at {directory}, skipping");
								break;
						}
						
						continue;
					}
					
					var bundlePath = Path.Combine(directory, platform);
					var catalogPath = Path.Combine(bundlePath, $"{modInfo.GetGUID()}.bin");
					
					if (!Directory.Exists(bundlePath) || !File.Exists(catalogPath))
					{
						Debug.LogWarning($"[ModLoader] Mod at {directory} does not have data for platform {platform}, skipping");
						continue;
					}

					foundMods.Add(modInfo, new Tuple<string, string>(directory, catalogPath));
					Debug.Log($"[ModLoader] Preloaded mod {modInfo.GetGUID()} {modInfo.Version} ({(modInfo.Disabled ? "Disabled" : "Enabled")})");
				}
				catch (Exception e)
				{
					Debug.LogError($"[ModLoader] Exception preloading mod at {directory}, {e}");
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
					Debug.LogWarning($"[ModLoader] Skipping loading mod {info.GetGUID()} {info.Version} at {directory} because multiple instances of it are installed");
					continue;
				}

				try
				{
					var mod = new Mod(info, directory, assetPath);
					mods.Add(mod);
					
					if (!mod.Info.Disabled)
						loadMod(mod);
				}
				catch (Exception e)
				{
					Debug.LogError($"[ModLoader] Exception loading mod at {directory}, {e}");
				}
			}
		}

		private bool loadCustomAssembly(Mod mod)
		{
			var info = mod.Info;
			
			var assemblyPath = Path.Combine(mod.Directory, $"{info.GetGUID()}.dll");
			if (!File.Exists(assemblyPath))
			{
				Debug.LogWarning($"[ModLoader] Could not find custom assembly for mod at {mod.Directory}, no content added");
				return false;
			}
			
			AssemblyDefinition assemblyDefinition;

			try
			{
				assemblyDefinition = AssemblyDefinition.ReadAssembly(assemblyPath);
			}
			catch (Exception e)
			{
				Debug.LogWarning($"[ModLoader] Failed to read custom assembly definition for mod at {mod.Directory}, no content added, {e}");
				return false;
			}

			var assemblyName = assemblyDefinition.Name.Name;
			if (assemblyName != info.GetGUID())
			{
				Debug.LogWarning($"[ModLoader] Invalid custom assembly name (should be {info.GetGUID()}, is {assemblyName}) for mod at {mod.Directory}, no content added");
				return false;
			}

			var references = assemblyDefinition.MainModule.AssemblyReferences;
			for (var i = 0; i < references.Count; i++)
			{
				var reference = references[i];
				
				if (isWhitelistedReference(reference))
					continue;

				Debug.LogWarning($"[ModLoader] Custom assembly references non-whitelisted reference {reference.FullName} for mod at {mod.Directory}, no content added");
				return false;
			}

			var currentReferences = AssemblyDefinition.ReadAssembly(typeof(ModLoader).Assembly.Location).MainModule.AssemblyReferences;

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

						if (isBlacklistedNamespaceOrType(reference.DeclaringType))
						{
							Debug.LogWarning($"[ModLoader] Custom assembly uses blacklisted namespace or type {reference.DeclaringType.Namespace}.{reference.DeclaringType.Name} for mod at {mod.Directory}, no content added");
							return false;
						}
					}
				}
			}

			using var stream = new MemoryStream();
			assemblyDefinition.Write(stream);
			stream.Position = 0;
			
			var bytes = stream.ToArray();
			
			mod.CustomAssemblyLoaded = true;
			Assembly.Load(bytes);
			
			return true;
		}
		
		private bool isAllowedModdedData(Type type)
		{
			return allowedModdedDatas.Contains(type);
		}

		private bool isWhitelistedReference(AssemblyNameReference reference)
		{
			return whitelistedReferences.Contains(reference.Name);
		}

		private bool isBlacklistedNamespaceOrType(TypeReference type)
		{
			return blacklistedNamespaces.Contains(type.Namespace) || blacklistedTypes.TryGetValue(type.Namespace, out var typesList) && typesList.Contains(type.Name);
		}
		
		#endregion
	}
}