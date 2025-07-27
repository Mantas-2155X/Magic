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
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEditor.SceneManagement;
using UnityEngine;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

namespace Modding.Editor
{
	public class ModdingTool : EditorWindow
	{
		[SerializeField]
		public Preset CurrentPreset;

		[SerializeField]
		public int CurrentTab = 1;

		private int selectedLanguage;
		private string addLanguage;
		
		private Vector2 presetsScrollPosition;
		private Vector2 setupAndBuildScrollPosition;
		private Vector2 localizationScrollPosition;

		private readonly List<Tuple<string, string>> initializedPresets = new ();
		
		private readonly Regex fileFilter = new (@"\W|_");
		private readonly Regex versionFilter = new (@"^(0|[1-9]\d*)(\.(0|[1-9]\d*)){0,3}$");
		
		private readonly string exportPath = "data/mods";
		private readonly string presetsPath = "Assets/Modding/Presets";
		
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
		
		private readonly BuildTarget[] buildTargets = { BuildTarget.StandaloneLinux64, BuildTarget.StandaloneWindows64 };
		
		[MenuItem("Modding/Modding Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<ModdingTool>(true);
			window.minSize = new Vector2(350, 300);
			window.Show();
			
			window.initializePresets();
		}

		public void OnGUI()
		{
			CurrentTab = GUILayout.Toolbar(CurrentTab, new [] { "Presets", "Setup & Build", "Localization" });

			if (CurrentPreset == null)
				CurrentPreset = CreateInstance<Preset>();
			
			switch (CurrentTab)
			{
				case 0:
					presets();
					break;
				case 1:
					setupAndBuild();
					break;
				case 2:
					localization();
					break;
			}
		}

		public void OnDestroy()
		{
			savePreset(true);
		}

		private void presets()
		{
			EditorGUILayout.LabelField($"Presets ({initializedPresets.Count})");

			presetsScrollPosition = GUILayout.BeginScrollView(presetsScrollPosition);

			for (var i = 0; i < initializedPresets.Count; i++)
			{
				GUILayout.BeginHorizontal();

				if (GUILayout.Button(initializedPresets[i].Item2))
					loadPreset(initializedPresets[i].Item1);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					deletePreset(initializedPresets[i].Item1);
					return;
				}
				
				GUILayout.EndHorizontal();
			}
			
			GUILayout.EndScrollView();
			
			GUILayout.FlexibleSpace();
			
			if (GUILayout.Button("Save Preset"))
				savePreset();
		}
		
		private void setupAndBuild()
		{
			CurrentPreset.Author = EditorGUILayout.TextField("Author", fileFilter.Replace(CurrentPreset.Author, ""));
			CurrentPreset.Name = EditorGUILayout.TextField("Name", fileFilter.Replace(CurrentPreset.Name, ""));
			CurrentPreset.Version = EditorGUILayout.TextField("Version", versionFilter.Match(CurrentPreset.Version).Value);
			
			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			CurrentPreset.CustomAssembly = EditorGUILayout.TextField("Custom Assembly", CurrentPreset.CustomAssembly);
			if (GUILayout.Button("Pick", GUILayout.Width(45)))
				CurrentPreset.CustomAssembly = EditorUtility.OpenFilePanel("Custom Assembly", "Assets", "dll");
			GUILayout.EndHorizontal();

			GUILayout.Space(5);
			
			GUILayout.BeginHorizontal();
			
			EditorGUILayout.LabelField($"Objects ({CurrentPreset.Objects.Count})");
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				CurrentPreset.Objects.Clear();
				return;
			}
			
			if (GUILayout.Button("+", GUILayout.Width(25)))
				CurrentPreset.Objects.Add(null);
			
			GUILayout.EndHorizontal();

			setupAndBuildScrollPosition = GUILayout.BeginScrollView(setupAndBuildScrollPosition);
			
			for (var i = 0; i < CurrentPreset.Objects.Count; i++)
			{
				GUILayout.BeginHorizontal();
				
				CurrentPreset.Objects[i] = (Data)EditorGUILayout.ObjectField(CurrentPreset.Objects[i], typeof(Data), false);
				
				if (GUILayout.Button("-", GUILayout.Width(25)))
				{
					CurrentPreset.Objects.RemoveAt(i);
					return;
				}

				if (CurrentPreset.Objects[i] != null)
				{
					var type = CurrentPreset.Objects[i].GetType();
					
					if (!allowedModdedDatas.Contains(type))
					{
						CurrentPreset.Objects[i] = null;
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

				var directory = $"{CurrentPreset.Author}.{CurrentPreset.Name}";
				var path = Path.Combine(exportPath, directory);
				
				var settings = AddressableAssetSettingsDefaultObject.Settings;

				if (Directory.Exists(path))
					Directory.Delete(path, true);
				
				Directory.CreateDirectory(path);

				var modInfo = new ObjectManager.ModInfo();
				modInfo.Author = CurrentPreset.Author;
				modInfo.Name = CurrentPreset.Name;
				modInfo.Version = CurrentPreset.Version;
				modInfo.Disabled = false;
				modInfo.UseCustomAssembly = !string.IsNullOrWhiteSpace(CurrentPreset.CustomAssembly);
				modInfo.Objects = new List<ObjectManager.ModInfo.ObjectInfo>();
				modInfo.Localizations = new List<ObjectManager.ModInfo.LocalizationInfo>();

				for (var i = 0; i < CurrentPreset.Objects.Count; i++)
				{
					var obj = CurrentPreset.Objects[i];

					var objectInfo = new ObjectManager.ModInfo.ObjectInfo();
					objectInfo.Type = obj.GetType().Name;
					objectInfo.Name = obj.Name;
					
					modInfo.Objects.Add(objectInfo);
				}

				for (var i = 0; i < CurrentPreset.Localizations.Count; i++)
				{
					var localization = CurrentPreset.Localizations[i];
					
					var localizationInfo = new ObjectManager.ModInfo.LocalizationInfo();
					localizationInfo.Language = localization.Language;
					localizationInfo.Entries = new Dictionary<string, string>();

					for (var k = 0; k < localization.Entries.Count; k++)
					{
						if (k >= CurrentPreset.Objects.Count)
							continue;
						
						var entry = localization.Entries[k];
						var obj = CurrentPreset.Objects[k];

						localizationInfo.Entries[obj.Name] = entry.Name;
						localizationInfo.Entries[obj.Description] = entry.Description;
					}
					
					modInfo.Localizations.Add(localizationInfo);
				}
				
				if (!string.IsNullOrWhiteSpace(CurrentPreset.CustomAssembly))
				{
					var fileInfo = new FileInfo(CurrentPreset.CustomAssembly);
					File.Copy(CurrentPreset.CustomAssembly, Path.Combine(path, fileInfo.Name));
				}
				
				File.WriteAllText(Path.Combine(path, "info.json"), JsonConvert.SerializeObject(modInfo, Formatting.Indented));

				var group = settings.CreateGroup($"{CurrentPreset.Author}.{CurrentPreset.Name}", false, false, false, null, typeof(BundledAssetGroupSchema), typeof(ContentUpdateGroupSchema));

				var restoreAssets = new Dictionary<string, AddressableAssetGroup>();
				var restoreScenes = new Dictionary<AddressableAssetEntry, string>();
				
				for (var i = 0; i < CurrentPreset.Objects.Count; i++)
				{
					var obj = CurrentPreset.Objects[i];
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
							
							entry.SetAddress($"Scenes/{CurrentPreset.Author}.{CurrentPreset.Name}.{obj.Name}");
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
				settings.BuiltInBundleCustomNaming = $"{CurrentPreset.Author}.{CurrentPreset.Name}".ToLower();
				settings.MonoScriptBundleNaming = MonoScriptBundleNaming.Custom;
				settings.MonoScriptBundleCustomNaming = $"{CurrentPreset.Author}.{CurrentPreset.Name}".ToLower();

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

					Directory.Move(buildTarget.ToString(), bundlePath);
					
					var binaries = Directory.GetFiles(bundlePath, "*.bin", SearchOption.TopDirectoryOnly);
					if (binaries.Length != 1)
					{
						Debug.LogError($"[ModdingTool] Binary count for platform {buildTarget} is not 1, something went wrong");
					}
					else
					{
						var fileInfo = new FileInfo(binaries[0]);
						
						File.Move(fileInfo.FullName, Path.Combine(fileInfo.DirectoryName!, $"{CurrentPreset.Author}.{CurrentPreset.Name}.bin"));
						File.Move($"{fileInfo.FullName[..^fileInfo.Extension.Length]}.hash", Path.Combine(fileInfo.DirectoryName!, $"{CurrentPreset.Author}.{CurrentPreset.Name}.hash"));
					}
				}

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
			}
		}

		private void localization()
		{
			GUILayout.BeginHorizontal();

			EditorGUILayout.LabelField($"Languages ({CurrentPreset.Localizations.Count})", GUILayout.Width(125));
			
			var selectLanguages = new string[CurrentPreset.Localizations.Count];
			
			for (var i = 0; i < selectLanguages.Length; i++)
				selectLanguages[i] = CurrentPreset.Localizations[i].Language;
			
			selectedLanguage = EditorGUILayout.Popup(selectedLanguage, selectLanguages);
			
			GUI.enabled = selectLanguages.Length > 0;
			var shouldRemove = GUILayout.Button("Remove", GUILayout.Width(85));
			GUI.enabled = true;
			
			if (shouldRemove)
			{
				CurrentPreset.Localizations.RemoveAt(selectedLanguage);
				selectedLanguage = 0;
				return;
			}
			
			if (GUILayout.Button("Clear", GUILayout.Width(45)))
			{
				CurrentPreset.Localizations.Clear();
				return;
			}
			
			GUILayout.EndHorizontal();
			
			GUILayout.BeginHorizontal();
			
			addLanguage = GUILayout.TextField(addLanguage);
			
			GUI.enabled = !string.IsNullOrWhiteSpace(addLanguage) && Array.IndexOf(selectLanguages, addLanguage) == -1 && Regex.IsMatch(addLanguage, "^[a-z]+$");
			var shouldAdd = GUILayout.Button("Add", GUILayout.Width(45));
			GUI.enabled = true;
			
			if (shouldAdd)
			{
				CurrentPreset.Localizations.Add(new LocalizationData
				{
					Language = addLanguage,
					Entries = new List<LocalizationDataEntry>()
				});
				
				addLanguage = "";
				selectedLanguage = CurrentPreset.Localizations.Count - 1;
			}
			
			GUILayout.EndHorizontal();
			
			if (CurrentPreset.Localizations.Count == 0)
				return;

			GUILayout.Space(5);

			var list = CurrentPreset.Localizations[selectedLanguage].Entries;
			
			EditorGUILayout.LabelField($"Entries ({CurrentPreset.Objects.Count * 2})");

			localizationScrollPosition = GUILayout.BeginScrollView(localizationScrollPosition);

			for (var i = 0; i < CurrentPreset.Objects.Count; i++)
			{
				if (i > list.Count - 1)
					list.Add(new LocalizationDataEntry());
				
				var obj = CurrentPreset.Objects[i];
				if (obj == null)
					continue;

				EditorGUIUtility.labelWidth = position.width / 2f - 10;
				
				var localizationEntry = list[i];
				localizationEntry.Name = EditorGUILayout.TextField(obj.Name, localizationEntry.Name);
				localizationEntry.Description = EditorGUILayout.TextField(obj.Description, localizationEntry.Description);
				
				EditorGUIUtility.labelWidth = 0;

				GUILayout.Space(5);
			}
			
			GUILayout.EndScrollView();
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
			if (string.IsNullOrWhiteSpace(CurrentPreset.Author))
			{
				GUILayout.Label("Author is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(CurrentPreset.Name))
			{
				GUILayout.Label("Name is empty or invalid");
				return false;
			}
			
			if (string.IsNullOrWhiteSpace(CurrentPreset.Version))
			{
				GUILayout.Label("Version is empty or invalid");
				return false;
			}
			
			if (CurrentPreset.Objects.Count == 0)
			{
				GUILayout.Label("No objects specified");
				return false;
			}
			
			if (!string.IsNullOrEmpty(CurrentPreset.CustomAssembly))
			{
				if (!CurrentPreset.CustomAssembly.EndsWith($"{CurrentPreset.Author}.{CurrentPreset.Name}.dll"))
				{
					GUILayout.Label("Custom assembly must be called Author.Name.dll");
					return false;
				}
				
				if (!File.Exists(CurrentPreset.CustomAssembly))
				{
					GUILayout.Label("Custom assembly does not exist");
					return false;
				}
			}
			
			for (var i = 0; i < CurrentPreset.Objects.Count; i++)
			{
				var obj = CurrentPreset.Objects[i];
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
			for (var i = 0; i < CurrentPreset.Objects.Count; i++)
			{
				if (CurrentPreset.Objects[i] is not SceneData sceneData)
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

		private void savePreset(bool autoSave = false)
		{
			if (!Directory.Exists(presetsPath))
				Directory.CreateDirectory(presetsPath);

			var savePath = Path.Combine(presetsPath, autoSave ? "autosave.asset" : $"{CurrentPreset.Author}.{CurrentPreset.Name}.{CurrentPreset.Version}.asset");
				
			if (File.Exists(savePath))
			{
				AssetDatabase.DeleteAsset(savePath);
				Debug.LogWarning($"[ModdingTool] Removed existing preset at {savePath}");
			}
			
			AssetDatabase.CreateAsset(Instantiate(CurrentPreset), savePath);
			Debug.Log($"[ModdingTool] Saved preset to {savePath}");
			
			initializePresets();
		}
		
		private void loadPreset(string path)
		{
			var preset = AssetDatabase.LoadAssetAtPath<Preset>(path);
			if (preset == null)
			{
				Debug.LogWarning($"[ModdingTool] Failed to load preset at {path}");
				return;
			}
			
			CurrentPreset = Instantiate(preset);
			Debug.Log($"[ModdingTool] Loaded preset from {path}");

			initializePresets();
		}

		private void deletePreset(string path)
		{
			if (!File.Exists(path) || !path.EndsWith(".asset"))
			{
				Debug.LogWarning($"[ModdingTool] Failed to delete preset at {path}");
				return;
			}

			AssetDatabase.DeleteAsset(path);
			Debug.Log($"[ModdingTool] Deleted preset at {path}");
			
			initializePresets();
		}
		
		private void initializePresets()
		{
			if (!Directory.Exists(presetsPath))
				Directory.CreateDirectory(presetsPath);

			initializedPresets.Clear();
			
			var fileNames = Directory.GetFiles(presetsPath, "*.asset", SearchOption.TopDirectoryOnly);
			for (var i = 0; i < fileNames.Length; i++)
			{
				var fileInfo = new FileInfo(fileNames[i]);
				if (!fileInfo.Exists)
				{
					Debug.LogWarning($"[ModdingTool] Preset at {fileNames[i]} not found");
					continue;
				}

				initializedPresets.Add(new Tuple<string, string>(fileNames[i], fileInfo.Name[..^fileInfo.Extension.Length]));
			}
		}
	}
}