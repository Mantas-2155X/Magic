using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Modding.EditorScripts;
using Modding.Infos;
using Newtonsoft.Json;
using ScriptableObjects;
using Tools;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Modding.Editor
{
	public partial class ModdingTool
	{
		private readonly Regex fileFilter = new (@"\W|_");
		private readonly Regex versionFilter = new (@"^(0|[1-9]\d*)(\.(0|[1-9]\d*)){0,3}$");
		
		private const string exportPath = "data/mods";
		
		private readonly List<Type> allowedModdedDatas = new ()
		{
			//typeof(AliveData),
			typeof(AttackData), 
			typeof(AudioData),
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
		
		private readonly BuildTarget[] buildTargets = { BuildTarget.StandaloneLinux64, BuildTarget.StandaloneWindows64 };

		private void drawSetupAndBuild()
		{
			State.Preset.Author = EditorGUILayout.TextField("Author", fileFilter.Replace(State.Preset.Author, ""));
			State.Preset.Name = EditorGUILayout.TextField("Name", fileFilter.Replace(State.Preset.Name, ""));
			State.Preset.Version = EditorGUILayout.TextField("Version", versionFilter.Match(State.Preset.Version).Value);
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			State.Preset.CustomAssembly = EditorGUILayout.TextField("Custom Assembly", State.Preset.CustomAssembly);
			if (GUILayout.Button("Pick", GUILayout.Width(45)))
				State.Preset.CustomAssembly = EditorUtility.OpenFilePanel("Custom Assembly", "Assets", "dll");
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			
			EditorGUILayout.LabelField($"Objects ({State.Preset.Objects.Count})");
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				State.Preset.Objects.Clear();
				return;
			}
			
			if (GUILayout.Button("+", GUILayout.Width(25)))
				State.Preset.Objects.Add(null);
			
			GUILayout.EndHorizontal();

			State.SetupAndBuildScrollPosition = GUILayout.BeginScrollView(State.SetupAndBuildScrollPosition);
			
			for (var i = 0; i < State.Preset.Objects.Count; i++)
			{
				GUILayout.BeginHorizontal();
				
				State.Preset.Objects[i] = (Data)EditorGUILayout.ObjectField(State.Preset.Objects[i], typeof(Data), false);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					State.Preset.Objects.RemoveAt(i);
					return;
				}

				if (State.Preset.Objects[i] != null)
				{
					var type = State.Preset.Objects[i].GetType();
					
					if (!allowedModdedDatas.Contains(type))
					{
						State.Preset.Objects[i] = null;
						Debug.LogWarning($"[ModdingTool] Objects of data {type} are not supported");
					}
				}
				
				GUILayout.EndHorizontal();
			}
			
			GUILayout.EndScrollView();
			
			GUILayout.FlexibleSpace();

			GUI.enabled = validate();
			var shouldBuild = GUILayout.Button("Build Mod");
			GUI.enabled = true;
			
			if (shouldBuild)
			{
				if (!validateOnBuild())
					return;

				if (!Directory.Exists(exportPath))
					Directory.CreateDirectory(exportPath);

				var directory = State.Preset.GetGUID();
				var path = Path.Combine(exportPath, directory);
				
				var settings = AddressableAssetSettingsDefaultObject.Settings;
				var hashField = settings.GetType().GetField("m_currentHash", BindingFlags.NonPublic | BindingFlags.Instance);

				var previousHash = hashField.GetValue(settings);

				if (Directory.Exists(path))
					Directory.Delete(path, true);
				
				Directory.CreateDirectory(path);

				var modInfo = new ModInfo();
				modInfo.Author = State.Preset.Author;
				modInfo.Name = State.Preset.Name;
				modInfo.Version = State.Preset.Version;
				modInfo.Disabled = false;
				modInfo.UseCustomAssembly = !string.IsNullOrWhiteSpace(State.Preset.CustomAssembly);
				modInfo.Objects = new List<ObjectInfo>();
				modInfo.Localizations = new List<LocalizationInfo>();

				for (var i = 0; i < State.Preset.Objects.Count; i++)
				{
					var obj = State.Preset.Objects[i];

					var objectInfo = new ObjectInfo();
					objectInfo.Type = obj.GetType().Name;
					objectInfo.Name = obj.Name;
					
					modInfo.Objects.Add(objectInfo);
				}

				for (var i = 0; i < State.Preset.Localizations.Count; i++)
				{
					var localization = State.Preset.Localizations[i];
					
					var localizationInfo = new LocalizationInfo();
					localizationInfo.Language = localization.Language;
					localizationInfo.Entries = new Dictionary<string, string>();

					for (var k = 0; k < localization.Entries.Count; k++)
					{
						if (k >= State.Preset.Objects.Count)
							continue;
						
						var entry = localization.Entries[k];
						var obj = State.Preset.Objects[k];

						localizationInfo.Entries[obj.Name] = entry.Name;
						localizationInfo.Entries[obj.Description] = entry.Description;
					}
					
					modInfo.Localizations.Add(localizationInfo);
				}
				
				if (!string.IsNullOrWhiteSpace(State.Preset.CustomAssembly))
				{
					var fileInfo = new FileInfo(State.Preset.CustomAssembly);
					File.Copy(State.Preset.CustomAssembly, Path.Combine(path, fileInfo.Name));
				}
				
				File.WriteAllText(Path.Combine(path, "info.json"), JsonConvert.SerializeObject(modInfo, Formatting.Indented));

				var group = settings.CreateGroup(State.Preset.GetGUID(), false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

				var restoreAssets = new Dictionary<string, AddressableAssetGroup>();
				var restoreScenes = new Dictionary<AddressableAssetEntry, string>();
				
				for (var i = 0; i < State.Preset.Objects.Count; i++)
				{
					var obj = State.Preset.Objects[i];
					if (obj == null)
						continue;

					var sceneReferences = new List<string>();
					
					var references = new List<string>();
					references.AddUnique(AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(obj)));
					references.AddUnique(obj.PrefabReference.AssetGUID);
						
					if (obj is ProjectileData projectileData)
					{
						references.AddUnique(projectileData.Decal.PrefabReference.AssetGUID);
					}
					else if (obj is SpellData spellData)
					{
						references.AddUnique(spellData.Cast.PrefabReference.AssetGUID);
						
						references.AddUnique(spellData.Projectile.PrefabReference.AssetGUID);
						references.AddUnique(spellData.Projectile.Decal.PrefabReference.AssetGUID);
						
						references.AddUnique(spellData.Attack.PrefabReference.AssetGUID);
					}
					else if (obj is ObjectData objectData)
					{
						references.AddUnique(objectData.BrokenPrefabReference.AssetGUID);
					}
					else if (obj is SceneData sceneData)
					{
						references.AddUnique(sceneData.Addressable.AssetGUID);
						sceneReferences.AddUnique(sceneData.Addressable.AssetGUID);
					}
					else if (obj is AudioData audioData)
					{
						references.AddUnique(audioData.ClipReference.AssetGUID);
					}

					for (var k = 0; k < references.Count; k++)
					{
						AddressableAssetGroup parentGroup;
						
						var entry = settings.FindAssetEntry(references[k]);
						if (entry != null)
						{
							parentGroup = entry.parentGroup;
							settings.MoveEntry(entry, group, false, false);
						}
						else
						{
							parentGroup = null;
							entry = settings.CreateOrMoveEntry(references[k], group, false, false);
						}

						if (sceneReferences.Contains(references[k]))
						{
							if (parentGroup != null)
								restoreScenes[entry] = entry.address;
							
							entry.SetAddress($"Scenes/{State.Preset.GetGUID()}.{obj.Name}");
						}
						
						restoreAssets[references[k]] = parentGroup;
					}
				}
				
				var removedGroups = removeGroups(group);
				
				var variable = settings.profileSettings.CreateValue("Mod", "[BuildTarget]");

				settings.DefaultGroup = group;
				
				var schema = group.GetSchema<BundledAssetGroupSchema>();
				schema.BuildPath.SetVariableByName(settings, "Mod");
				schema.LoadPath.SetVariableByName(settings, "Mod");
				schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID;
				schema.BundleNaming = BundledAssetGroupSchema.BundleNamingStyle.NoHash;
				schema.UseAssetBundleCrc = false;

				var previousRemoteCatalogBuildPath = settings.RemoteCatalogBuildPath.Id;
				var previousRemoteCatalogLoadPath = settings.RemoteCatalogLoadPath.Id;
				var previousBuildRemoteCatalog = settings.BuildRemoteCatalog;
				var previousBuiltInBundleNaming = settings.BuiltInBundleNaming;
				var previousBuiltInBundleCustomNaming = settings.BuiltInBundleCustomNaming;
				var previousMonoScriptBundleNaming = settings.MonoScriptBundleNaming;
				var previousMonoScriptBundleCustomNaming = settings.MonoScriptBundleCustomNaming;

				settings.RemoteCatalogBuildPath.SetVariableByName(settings, "Mod");
				settings.RemoteCatalogLoadPath.SetVariableByName(settings, "Mod");
				settings.BuildRemoteCatalog = true;
				settings.BuiltInBundleNaming = BuiltInBundleNaming.Custom;
				settings.BuiltInBundleCustomNaming = State.Preset.GetGUID().ToLower();
				settings.MonoScriptBundleNaming = MonoScriptBundleNaming.Custom;
				settings.MonoScriptBundleCustomNaming = State.Preset.GetGUID().ToLower();

				var previousBuildTarget = EditorUserBuildSettings.activeBuildTarget;
				
				WindowState.Save(State);

				for (var i = 0; i < buildTargets.Length; i++)
				{
					var buildTarget = buildTargets[i];
					var bundlePath = Path.Combine(path, buildTarget.ToString());
					
					if (Directory.Exists(bundlePath))
						Directory.Delete(bundlePath, true);

					if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
						EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, buildTarget);
					
					AddressableAssetSettings.BuildPlayerContent();

					Directory.Move(buildTarget.ToString(), bundlePath);
					
					var binaries = Directory.GetFiles(bundlePath, "*.bin", SearchOption.TopDirectoryOnly);
					if (binaries.Length != 1)
					{
						Debug.LogError($"[ModdingTool] Binary count for platform {buildTarget} is not 1, something went wrong");
					}
					else
					{
						var fileInfo = new FileInfo(binaries[0]);
						
						File.Move(fileInfo.FullName, Path.Combine(fileInfo.DirectoryName!, $"{State.Preset.GetGUID()}.bin"));
						File.Move($"{fileInfo.FullName[..^fileInfo.Extension.Length]}.hash", Path.Combine(fileInfo.DirectoryName!, $"{State.Preset.GetGUID()}.hash"));
					}
				}
				
				State = WindowState.Load();
				WindowState.Delete();

				if (EditorUserBuildSettings.activeBuildTarget != previousBuildTarget)
					EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, previousBuildTarget);

				settings.RemoteCatalogBuildPath.SetVariableById(settings, previousRemoteCatalogBuildPath);
				settings.RemoteCatalogLoadPath.SetVariableById(settings, previousRemoteCatalogLoadPath);
				settings.BuildRemoteCatalog = previousBuildRemoteCatalog;
				settings.BuiltInBundleNaming = previousBuiltInBundleNaming;
				settings.BuiltInBundleCustomNaming = previousBuiltInBundleCustomNaming;
				settings.MonoScriptBundleNaming = previousMonoScriptBundleNaming;
				settings.MonoScriptBundleCustomNaming = previousMonoScriptBundleCustomNaming;

				foreach (var pair in restoreAssets)
				{
					if (pair.Value != null)
						settings.MoveEntry(settings.FindAssetEntry(pair.Key), pair.Value, false, false);
					else
						settings.RemoveAssetEntry(pair.Key);
				}

				foreach (var pair in restoreScenes)
					pair.Key.SetAddress(pair.Value);
				
				settings.RemoveGroup(group);
				
				settings.profileSettings.RemoveValue(variable);
				
				restoreGroups(removedGroups);
				
				hashField.SetValue(settings, previousHash);
			}
		}
		
		private (List<AddressableAssetGroup>, string) removeGroups(AddressableAssetGroup ignoreGroup)
		{
			var list = new List<AddressableAssetGroup>();
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			
			var defaultGroup = settings.DefaultGroup == null ? null : settings.DefaultGroup.Guid;

			for (var i = settings.groups.Count - 1; i >= 0; i--)
			{
				var group = settings.groups[i];
				if (group == null || group.ReadOnly || group == ignoreGroup)
					continue;

				list.Add(group);
				settings.groups.RemoveAt(i);
			}

			return (list, defaultGroup);
		}

		private void restoreGroups((List<AddressableAssetGroup>, string) tuple)
		{
			var list = tuple.Item1;
			var settings = AddressableAssetSettingsDefaultObject.Settings;
			
			for (var i = 0; i < list.Count; i++)
			{
				var group = list[i];
				
				if (settings.FindGroup(group.Name) != null)
					continue;
				
				settings.groups.Add(group);
			}
			
			settings.GetType().GetField("m_DefaultGroup", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(settings, tuple.Item2);
		}
		
		private bool validate()
		{
			if (string.IsNullOrWhiteSpace(State.Preset.Author))
			{
				GUILayout.Label("Author is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(State.Preset.Name))
			{
				GUILayout.Label("Name is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(State.Preset.Version))
			{
				GUILayout.Label("Version is empty or invalid");
				return false;
			}
			
			if (State.Preset.Objects.Count == 0)
			{
				GUILayout.Label("No objects specified");
				return false;
			}
			
			if (!string.IsNullOrEmpty(State.Preset.CustomAssembly))
			{
				if (!State.Preset.CustomAssembly.EndsWith($"{State.Preset.GetGUID()}.dll"))
				{
					GUILayout.Label("Custom assembly must be called Author.Name.dll");
					return false;
				}
				
				if (!File.Exists(State.Preset.CustomAssembly))
				{
					GUILayout.Label("Custom assembly does not exist");
					return false;
				}
			}
			
			for (var i = 0; i < State.Preset.Objects.Count; i++)
			{
				var obj = State.Preset.Objects[i];
				if (obj == null)
				{
					GUILayout.Label("Null objects are not allowed");
					return false;
				}

				if (string.IsNullOrWhiteSpace(obj.Name) || string.IsNullOrWhiteSpace(obj.Description))
				{
					GUILayout.Label("All objects must have a name and description");
					return false;
				}
			}

			return true;
		}

		private bool validateOnBuild()
		{
			for (var i = 0; i < State.Preset.Objects.Count; i++)
			{
				if (State.Preset.Objects[i] is not SceneData sceneData)
					continue;

				if (string.IsNullOrWhiteSpace(sceneData.Addressable.AssetGUID))
				{
					Debug.LogError("[ModdingTool] Scene addressable must be assigned");
					return false;
				}

				World.World world = null;
				
				var scenePath = AssetDatabase.GUIDToAssetPath(sceneData.Addressable.AssetGUID);
				var scene = SceneManager.GetSceneByPath(scenePath);
				
				var shouldClose = true;
				
				if (!scene.isLoaded)
					scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
				else
					shouldClose = false;

				var rootObjects = scene.GetRootGameObjects();
				for (var k = 0; k < rootObjects.Length; k++)
				{
					var rootObject = rootObjects[k];
					if (!rootObject.TryGetComponent(out world))
						continue;

					break;
				}

				if (world == null)
				{
					Debug.LogError("[ModdingTool] Scene must have a World component");
					
					if (shouldClose)
						EditorSceneManager.CloseScene(scene, true);
					
					return false;
				}
				
				if (world.SpawnPoints == null || world.Characters == null || world.Ragdolls == null || world.Attacks == null || world.Casts == null || world.Projectiles == null || world.Objects == null || world.Decals == null)
				{
					Debug.LogError("[ModdingTool] World component inside Scene must have all Transforms assigned");
					
					if (shouldClose)
						EditorSceneManager.CloseScene(scene, true);
					
					return false;
				}
				
				if (shouldClose)
					EditorSceneManager.CloseScene(scene, true);
			}

			return true;
		}
	}
}