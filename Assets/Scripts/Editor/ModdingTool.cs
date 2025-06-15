using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Managers;
using Newtonsoft.Json;
using ScriptableObjects;
using Tools;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Editor
{
	public class ModdingTool : EditorWindow
	{
		[SerializeField]
		public string Author = "MyName";
		
		[SerializeField]
		public string Name = "ModName";

		[SerializeField]
		public string Version = "1.0.0";

		[SerializeField]
		public string CustomAssembly = "";
		
		[SerializeField]
		public List<Data> Objects = new ();
		
		private Vector2 scrollPosition;
		
		private readonly Regex fileFilter = new (@"\W|_");
		private readonly Regex versionFilter = new (@"^(0|[1-9]\d*)(\.(0|[1-9]\d*)){0,3}$");
		
		private readonly string exportPath = "data/mods";
		
		private readonly List<Type> allowedModdedDatas = new ()
		{
			typeof(AttackData), 
			typeof(CastData), 
			typeof(DecalData), 
			typeof(ProjectileData), 
			typeof(SpellData), 
			typeof(WearableData)
		};
		
		private readonly BuildTarget[] buildTargets = { BuildTarget.StandaloneLinux64, BuildTarget.StandaloneWindows64 };
		
		[MenuItem("Modding/Modding Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<ModdingTool>(true);
			window.minSize = new Vector2(300, 300);
			window.Show();
		}

		public void OnGUI()
		{
			Author = EditorGUILayout.TextField("Author", fileFilter.Replace(Author, ""));
			Name = EditorGUILayout.TextField("Name", fileFilter.Replace(Name, ""));
			Version = EditorGUILayout.TextField("Version", versionFilter.Match(Version).Value);
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			CustomAssembly = EditorGUILayout.TextField("Custom Assembly", CustomAssembly);
			if (GUILayout.Button("Pick", GUILayout.Width(45)))
				CustomAssembly = EditorUtility.OpenFilePanel("Custom Assembly", "Assets", "dll");
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			
			Objects ??= new List<Data>();
			
			GUILayout.BeginHorizontal();
			
			EditorGUILayout.LabelField($"Objects ({Objects.Count})");
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				Objects.Clear();
				return;
			}
			
			if (GUILayout.Button("+", GUILayout.Width(25)))
				Objects.Add(null);
			
			GUILayout.EndHorizontal();

			scrollPosition = GUILayout.BeginScrollView(scrollPosition);
			
			for (var i = 0; i < Objects.Count; i++)
			{
				GUILayout.BeginHorizontal();
				
				Objects[i] = (Data)EditorGUILayout.ObjectField(Objects[i], typeof(Data), false);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					Objects.RemoveAt(i);
					return;
				}

				if (Objects[i] != null)
				{
					var type = Objects[i].GetType();
					
					if (!allowedModdedDatas.Contains(type))
					{
						Objects[i] = null;
						Debug.LogWarning($"[ModdingTools] Objects of data {type} are not supported");
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
				if (!Directory.Exists(exportPath))
					Directory.CreateDirectory(exportPath);

				var directory = $"{Author}.{Name}";
				var path = Path.Combine(exportPath, directory);
				
				var settings = AddressableAssetSettingsDefaultObject.Settings;

				if (!Directory.Exists(path))
					Directory.CreateDirectory(path);

				var modInfo = new ObjectManager.ModInfo();
				modInfo.Author = Author;
				modInfo.Name = Name;
				modInfo.Version = Version;
				modInfo.Disabled = false;
				modInfo.UseCustomAssembly = !string.IsNullOrWhiteSpace(CustomAssembly);
				modInfo.Objects = new List<ObjectManager.ModInfo.ObjectInfo>();

				for (var i = 0; i < Objects.Count; i++)
				{
					var obj = Objects[i];

					var objectInfo = new ObjectManager.ModInfo.ObjectInfo();
					objectInfo.Type = obj.GetType().Name;
					objectInfo.Name = obj.Name;
					
					modInfo.Objects.Add(objectInfo);
				}
				
				if (!string.IsNullOrWhiteSpace(CustomAssembly))
				{
					var fileInfo = new FileInfo(CustomAssembly);
					File.Copy(CustomAssembly, Path.Combine(path, fileInfo.Name));
				}
				
				File.WriteAllText(Path.Combine(path, "info.json"), JsonConvert.SerializeObject(modInfo, Formatting.Indented));

				var group = settings.CreateGroup($"{Author}.{Name}", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

				var restoreAssets = new Dictionary<string, AddressableAssetGroup>();
				
				for (var i = 0; i < Objects.Count; i++)
				{
					var obj = Objects[i];
					if (obj == null)
						continue;

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
							settings.CreateOrMoveEntry(references[k], group, false, false);
						}
						
						restoreAssets[references[k]] = parentGroup;
					}
				}
				
				var removedGroups = removeGroups(group);
				
				var variable = settings.profileSettings.CreateValue("Mod", $"data/mods/{Author}.{Name}/[BuildTarget]");

				settings.DefaultGroup = group;
				
				var schema = group.GetSchema<BundledAssetGroupSchema>();
				schema.BuildPath.SetVariableByName(settings, "Mod");
				schema.LoadPath.SetVariableByName(settings, "Mod");
				schema.InternalIdNamingMode = BundledAssetGroupSchema.AssetNamingMode.GUID;

				var previousRemoteCatalogBuildPath = settings.RemoteCatalogBuildPath.Id;
				var previousRemoteCatalogLoadPath = settings.RemoteCatalogLoadPath.Id;
				var previousBuildRemoteCatalog = settings.BuildRemoteCatalog;

				settings.RemoteCatalogBuildPath.SetVariableByName(settings, "Mod");
				settings.RemoteCatalogLoadPath.SetVariableByName(settings, "Mod");
				settings.BuildRemoteCatalog = true;

				var previousBuildTarget = EditorUserBuildSettings.activeBuildTarget;
				
				for (var i = 0; i < buildTargets.Length; i++)
				{
					var buildTarget = buildTargets[i];
					var bundlePath = Path.Combine(path, buildTarget.ToString());
					
					if (Directory.Exists(bundlePath))
						Directory.Delete(bundlePath, true);

					if (EditorUserBuildSettings.activeBuildTarget != buildTarget)
						EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, buildTarget);
					
					AddressableAssetSettings.BuildPlayerContent();

					var binaries = Directory.GetFiles(bundlePath, "*.bin", SearchOption.TopDirectoryOnly);
					if (binaries.Length != 1)
					{
						Debug.LogError($"[ModdingTool] Binary count for platform {buildTarget} is not 1, something went wrong");
					}
					else
					{
						var fileInfo = new FileInfo(binaries[0]);
						
						File.Move(fileInfo.FullName, Path.Combine(fileInfo.DirectoryName!, $"{Author}.{Name}.bin"));
						File.Move($"{fileInfo.FullName[..^fileInfo.Extension.Length]}.hash", Path.Combine(fileInfo.DirectoryName!, $"{Author}.{Name}.hash"));
					}
				}

				if (EditorUserBuildSettings.activeBuildTarget != previousBuildTarget)
					EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, previousBuildTarget);

				settings.RemoteCatalogBuildPath.SetVariableById(settings, previousRemoteCatalogBuildPath);
				settings.RemoteCatalogLoadPath.SetVariableById(settings, previousRemoteCatalogLoadPath);
				settings.BuildRemoteCatalog = previousBuildRemoteCatalog;

				foreach (var pair in restoreAssets)
				{
					if (pair.Value != null)
						settings.MoveEntry(settings.FindAssetEntry(pair.Key), pair.Value, false, false);
					else
						settings.RemoveAssetEntry(pair.Key);
				}
				
				settings.RemoveGroup(group);
				
				settings.profileSettings.RemoveValue(variable);
				
				restoreGroups(removedGroups);
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
			if (string.IsNullOrWhiteSpace(Author))
			{
				GUILayout.Label("Author is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(Name))
			{
				GUILayout.Label("Name is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(Version))
			{
				GUILayout.Label("Version is empty or invalid");
				return false;
			}
			
			if (Objects.Count == 0)
			{
				GUILayout.Label("No objects specified");
				return false;
			}
			
			if (!string.IsNullOrEmpty(CustomAssembly))
			{
				if (!CustomAssembly.EndsWith($"{Author}.{Name}.dll"))
				{
					GUILayout.Label("Custom assembly must be called Author.Name.dll");
					return false;
				}
				
				if (!File.Exists(CustomAssembly))
				{
					GUILayout.Label("Custom assembly does not exist");
					return false;
				}
			}
			
			for (var i = 0; i < Objects.Count; i++)
			{
				var obj = Objects[i];
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
	}
}