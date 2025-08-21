using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using AI.Interfaces;
using Combat.Decals.Interfaces;
using Cysharp.Threading.Tasks;
using Modding;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using Tools;
using UI;
using UI.Enums;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace Managers
{
	// TODO (impl):
	// (Objects) BaseElevator, BaseConveyor
	// (AI) Off mesh link travelling
	
	// TODO (test):
	// (AI) Grabbing shrink, Patrol Already Waited, Switch cast cooldown
	
	public class StateManager
	{
		private static StateManager instance;
		public static StateManager Instance
		{
			get
			{
				if (instance != null)
					return instance;
				
				instance = new StateManager();
				instance.initializeManager();
				instance.initializeSaves();
				
				return instance;
			}
		}

		public string Path { get; private set; } = "data/saves";
		
		#region Registered Objects (Global)
		
		private readonly Dictionary<string, IIdentifiable> registeredObjects = new ();

		public Dictionary<string, IIdentifiable> GetRegisteredObjects()
		{
			return registeredObjects;
		}

		public IIdentifiable GetRegisteredObject(string objectID)
		{
			if (string.IsNullOrWhiteSpace(objectID))
				return null;
			
			if (registeredObjects.TryGetValue(objectID, out var obj))
				return obj;

			return null;
		}

		public void RegisterObject(IIdentifiable identifiable)
		{
			registerObject(identifiable, identifiable.ObjectID);

			if (identifiable is not ISaveable saveable)
				return;

			saveable.OriginalScene = SceneManager.Instance.GetCurrentSceneData().Name;
		}
		
		public void UnregisterObject(IIdentifiable identifiable)
		{
			if (string.IsNullOrWhiteSpace(identifiable.ObjectID))
				return;

			registeredObjects.Remove(identifiable.ObjectID);
		}
		
		public string ChangeObjectID(IIdentifiable identifiable, string newObjectID)
		{
			UnregisterObject(identifiable);
			registerObject(identifiable, newObjectID);

			return newObjectID;
		}

		#endregion

		#region Destroyed Objects (Saving State)

		private readonly List<string> destroyedObjects = new ();
		private readonly List<string> destroyedComponents = new ();
		
		private readonly List<string> killedAlives = new ();

		public List<string> GetDestroyedObjects()
		{
			return destroyedObjects;
		}
		
		public List<string> GetDestroyedComponents()
		{
			return destroyedComponents;
		}
		
		public List<string> GetKilledAlives()
		{
			return killedAlives;
		}

		public void RegisterDestroyedObject(string objectID)
		{
			destroyedObjects.AddUnique(objectID);
		}

		public void RegisterDestroyedComponent(string objectID)
		{
			destroyedComponents.AddUnique(objectID);
		}

		public void RegisterKilledAlive(string objectID)
		{
			killedAlives.AddUnique(objectID);
		}

		#endregion
		
		#region Save & Load

		private readonly Dictionary<string, PartialSaveData> availableSaves = new ();

		public Dictionary<string, PartialSaveData> GetSaves()
		{
			return availableSaves;
		}
		
		public void AutoSave(bool notify)
		{
			var currentSceneData = SceneManager.Instance.GetCurrentSceneData();
			var removeSaves = new List<string>();
			
			var saves = GetSaves();
			foreach (var (filePath, saveData) in saves)
			{
				if (currentSceneData.Name != saveData.Scene || !saveData.AutoSave)
					continue;
				
				removeSaves.Add(filePath);
			}

			Debug.Log("[StateManager] Autosave called, saving");
			
			if (!Save(out _, true))
				return;

			if (notify)
				Player.Instance.Notice.ShowMessage(ENoticePresetFlags.AutoSave, 1.5f);
			
			for (var i = removeSaves.Count - 1; i >= 0; i--)
			{
				Debug.Log($"[StateManager] Removing previous autosave {removeSaves[i]}");
				Delete(removeSaves[i]);
			}
		}

		public PartialSaveData GetLatestSave()
		{
			var currentSceneData = SceneManager.Instance.GetCurrentSceneData();

			var searchScenes = new List<string>();
			searchScenes.Add(currentSceneData.Name);

			SceneManager.Instance.GetRelatedSceneNames(currentSceneData, searchScenes);
			
			var allSaves = GetSaves();
			var validSaves = new List<Tuple<string, PartialSaveData>>();

			foreach (var pair in allSaves)
			{
				if (!searchScenes.Contains(pair.Value.Scene))
					continue;
			
				validSaves.Add(new Tuple<string, PartialSaveData>(pair.Key, pair.Value));
			}

			if (validSaves.Count == 0)
				return null;
			
			validSaves.Sort((x, y) => y.Item2.SavedTime.CompareTo(x.Item2.SavedTime));
			return validSaves[0].Item2;
		}
		
		public bool Save(out SaveData save, bool isAutoSave = false, bool writeToFile = true)
		{
			var startTime = Time.realtimeSinceStartup;
			var sceneManager = SceneManager.Instance;
			
			var currentSceneData = sceneManager.GetCurrentSceneData();
			if (!currentSceneData.SupportsSaving)
			{
				save = null;
				return false;
			}
			
			var data = new SaveData();
			data.Scene = currentSceneData.Name;
			data.AutoSave = isAutoSave;
			data.SavedTime = DateTimeOffset.Now;
			
			data.DestroyedObjects = new List<string>();
			for (var i = 0; i < destroyedObjects.Count; i++)
				data.DestroyedObjects.Add(destroyedObjects[i]);
			
			data.DestroyedComponents = new List<string>();
			for (var i = 0; i < destroyedComponents.Count; i++)
				data.DestroyedComponents.Add(destroyedComponents[i]);
			
			data.KilledAlives = new List<string>();
			for (var i = 0; i < killedAlives.Count; i++)
				data.KilledAlives.Add(killedAlives[i]);
			
			data.Items = new Dictionary<string, SaveData.SaveItem>();
			foreach (var pair in savedItems)
				data.Items.Add(pair.Key, pair.Value);
			
			var characters = World.World.Instance.Characters;
			var ragdolls = World.World.Instance.Ragdolls;
			
			var gameAssembly = typeof(StateManager).Assembly;

			var components = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
				if (component is not ISaveable saveable)
					continue;
				
				// Skip what's not supported
				if (!saveable.ShouldSave)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} is not marked saveable, skipping");
					continue;
				}
				
				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} has no Object ID, skipping");
					continue;
				}
				
				// Don't save destroyed data
				if (destroyedObjects.Contains(saveable.ObjectID))
					continue;

				// Don't save killed alives data
				if (saveable is IAlive iAlive && (!iAlive.IsAlive || killedAlives.Contains(iAlive.ObjectID)))
					continue;

				// Make sure there is a root transform
				var root = component.transform.root;
				if (root == null)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} does not have a root, skipping");
					continue;
				}

				// Prevent saving ragdolls
				if (root == ragdolls)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} is a ragdoll, skipping");
					continue;
				}
				
				// Prevent saving stuff like gibs on characters
				if (root == characters && saveable is not IAlive and not IDecal)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} is not saved on characters, skipping");
					continue;
				}
				
				// Keep data of destroyed components as it might hold stuff outside of the component and be used
				
				try
				{
					var item = new SaveData.SaveItem
					{
						LoadType = saveable.LoadType,
						LoadTiming = saveable.LoadTiming,
						OriginalScene = saveable.OriginalScene,
						TransferredScene = saveable.TransferredScene
					};

					// If it was transferred it may or may not exist in the other scene, grab both create and modify
					if (!string.IsNullOrEmpty(saveable.TransferredScene))
					{
						var saveableType = saveable.GetType();
						item.CreateData = new Tuple<string, JObject>(saveableType.Assembly == gameAssembly ? saveableType.FullName : saveableType.AssemblyQualifiedName, saveable.GetCreation());
						item.ModifyData = saveable.GetModifications();
					}
					else
					{
						switch (saveable.LoadType)
						{
							case ELoadType.Create:
							{
								var saveableType = saveable.GetType();
								item.CreateData = new Tuple<string, JObject>(saveableType.Assembly == gameAssembly ? saveableType.FullName : saveableType.AssemblyQualifiedName, saveable.GetCreation());
								break;
							}
							case ELoadType.Modify:
							{
								item.ModifyData = saveable.GetModifications();
								break;
							}
						}
					}
					
					data.Items[saveable.ObjectID] = item;
				}
				catch (Exception e)
				{
					Debug.LogError($"[StateManager] Failed saving {saveable.GetType().Name} state for {TransformTools.GetFullPath(component.transform)} ({saveable.ObjectID}), {e}");
				}
			}

			var saveData = JsonConvert.SerializeObject(data, Formatting.Indented);
			if (string.IsNullOrEmpty(saveData))
			{
				Debug.LogError("[StateManager] Not saving save data as the object failed to serialize");
				
				save = null;
				return false;
			}

			var modifications = 0;
			var creations = 0;

			foreach (var pair in data.Items)
			{
				switch (pair.Value.LoadType)
				{
					case ELoadType.Create:
						creations++;
						break;
					case ELoadType.Modify:
						modifications++;
						break;
				}
			}

			if (!writeToFile)
			{
				save = data;
				return true;
			}
			
			Debug.Log($"[StateManager] Save Statistics ({(Time.realtimeSinceStartup - startTime) * 1000}ms): Destroyed Components {data.DestroyedComponents.Count}, Destroyed Objects {data.DestroyedObjects.Count}, Items {data.Items.Count}, Creations {creations}, Modifications {modifications}, Killed Alives {data.KilledAlives.Count}");
			
			File.WriteAllText(System.IO.Path.Combine(Path, $"{data.SavedTime:yyyy_MM_dd_HH_mm_ss_fff}.json"), saveData);
			
			initializeSaves();
			
			save = data;
			return true;
		}

		public void Load(SaveData data, bool useLoadingScreen = true)
		{
			LoadAsync(data, useLoadingScreen).Forget();
		}

		public async UniTask LoadAsync(SaveData data, bool useLoadingScreen = true)
		{
			var sceneManager = SceneManager.Instance;
			
			var sceneData = ObjectManager.Instance.GetData<SceneData>(data.Scene);
			if (!sceneData.SupportsSaving)
				return;

			lastSaveData = data;
			
			if (sceneData == sceneManager.GetCurrentSceneData())
				await sceneManager.ReloadSceneAsync(useLoadingScreen, useLoadingScreen, true, waitForGI: useLoadingScreen);
			else
				await sceneManager.ChangeSceneAsync(sceneData, useLoadingScreen, useLoadingScreen, true, waitForGI: useLoadingScreen);

			for (var i = data.DestroyedObjects.Count - 1; i >= 0; i--)
			{
				var destroyedObject = GetRegisteredObject(data.DestroyedObjects[i]);
				if (destroyedObject.IsNull())
					continue;

				Object.Destroy(destroyedObject.GetGameObject());
			}
			
			var killAlives = new List<IAlive>();
			
			loadStage(data, ELoadTiming.Normal, killAlives);
			loadStage(data, ELoadTiming.Alives, killAlives);
			loadStage(data, ELoadTiming.Late, killAlives);
			loadStage(data, ELoadTiming.VeryLate, killAlives);

			for (var i = data.DestroyedComponents.Count - 1; i >= 0; i--)
			{
				var destroyedComponent = GetRegisteredObject(data.DestroyedComponents[i]);
				if (destroyedComponent.IsNull())
					continue;

				Object.Destroy((Component)destroyedComponent);
			}
			
			for (var i = killAlives.Count - 1; i >= 0; i--)
				killAlives[i].Kill(null, true);
		}
		
		public void Load(PartialSaveData partialData, bool useLoadingScreen = true)
		{
			LoadAsync(partialData, useLoadingScreen).Forget();
		}

		public async UniTask LoadAsync(PartialSaveData partialData, bool useLoadingScreen = true)
		{
			var file = "";
			
			foreach (var pair in availableSaves)
			{
				if (partialData != pair.Value)
					continue;

				file = pair.Key;
				break;
			}
			
			if (string.IsNullOrEmpty(file) || !File.Exists(file))
			{
				Debug.LogWarning("[StateManager] Partial save data does not have a file");
				return;
			}
			
			var text = File.ReadAllText(file);
			if (string.IsNullOrWhiteSpace(text))
			{
				Debug.LogWarning($"[StateManager] Save file {file} is empty, skipping");
				return;
			}

			var data = JsonConvert.DeserializeObject<SaveData>(text);
			if (data == null)
			{
				Debug.LogWarning($"[StateManager] Save file {file} failed to deserialize, skipping");
				return;
			}
			
			await LoadAsync(data, useLoadingScreen);
		}

		public void Delete(string path)
		{
			if (!availableSaves.ContainsKey(path))
			{
				Debug.LogError("[StateManager] Not deleting save data as the file is not part of available saves");
				return;
			}
			
			if (!File.Exists(path))
			{
				Debug.LogError("[StateManager] Not deleting save data as the file does not exist");
				return;
			}

			File.Delete(path);
			
			initializeSaves();
		}

		#endregion

		#region Internals

		private readonly Dictionary<string, SaveData.SaveItem> savedItems = new ();

		private SaveData lastSaveData;

		private void registerObject(IIdentifiable identifiable, string objectID)
		{
			if (string.IsNullOrWhiteSpace(objectID))
				return;
			
			registeredObjects[objectID] = identifiable;
		}

		private void initializeManager()
		{
			SceneManager.OnPreSceneLoadEvent.AddListener(instance.onPreSceneLoad);
		}
		
		private void initializeSaves()
		{
			if (!Directory.Exists(Path))
				Directory.CreateDirectory(Path);

			availableSaves.Clear();

			var files = Directory.GetFiles(Path, "*.json");
			for (var i = 0; i < files.Length; i++)
			{
				var file = files[i];

				var text = File.ReadAllText(file);
				if (string.IsNullOrWhiteSpace(text))
				{
					Debug.LogWarning($"[StateManager] Save file {file} is empty, skipping");
					continue;
				}

				var data = JsonConvert.DeserializeObject<PartialSaveData>(text);
				if (data == null)
				{
					Debug.LogWarning($"[StateManager] Save file {file} failed to deserialize, skipping");
					continue;
				}
				
				availableSaves[file] = data;
			}
		}

		private void onPreSceneLoad(SceneData scene)
		{
			destroyedObjects.Clear();
			destroyedComponents.Clear();
			killedAlives.Clear();
			savedItems.Clear();

			if (lastSaveData == null)
				return;

			var destroyedObjectsList = lastSaveData.DestroyedObjects;
			for (var i = 0; i < destroyedObjectsList.Count; i++)
				destroyedObjects.Add(destroyedObjectsList[i]);

			var destroyedComponentsList = lastSaveData.DestroyedComponents;
			for (var i = 0; i < destroyedComponentsList.Count; i++)
				destroyedComponents.Add(destroyedComponentsList[i]);

			var killedAlivesList = lastSaveData.KilledAlives;
			for (var i = 0; i < killedAlivesList.Count; i++)
				killedAlives.Add(killedAlivesList[i]);

			var savedItemsDict = lastSaveData.Items;
			foreach (var pair in savedItemsDict)
				savedItems.Add(pair.Key, pair.Value);
			
			lastSaveData = null;
		}
		
		private void loadStage(SaveData data, ELoadTiming timing, List<IAlive> killAlives)
		{
			var saveables = new Dictionary<string, ISaveable>();

			var components = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
				if (component is not ISaveable saveable)
					continue;

				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
					continue;

				saveables[saveable.ObjectID] = saveable;
			}

			var modNames = new List<string>();
			
			var modInfos = ModLoader.Instance.GetModInfos();
			for (var i = 0; i < modInfos.Count; i++)
			{
				var modInfo = modInfos[i];
				if (modInfo == null || modInfo.Disabled)
					continue;

				modNames.AddUnique(modInfo.GetGUID());
			}
			
			var currentSceneName = SceneManager.Instance.GetCurrentSceneData().Name;

			foreach (var pair in data.Items)
			{
				var item = pair.Value;
				if (item.LoadTiming != timing)
					continue;

				var objectID = pair.Key;

				if (registeredObjects.TryGetValue(objectID, out var registeredObject) && !string.IsNullOrEmpty(item.TransferredScene))
				{
					// Moving back to original scene of the object
					if (item.OriginalScene == currentSceneName)
					{
						// Object moved from original scene to another and did not come back, destroy the original object
						if (item.TransferredScene != currentSceneName)
						{
							Object.DestroyImmediate(registeredObject.GetGameObject());
							continue;
						}
					
						// Object moved from original scene to another and came back. Destroy the original object and create/modify the moved one
						if (item.LoadType == ELoadType.Create)
							Object.DestroyImmediate(registeredObject.GetGameObject());
					}
					else
					{
						// Object moved in already exists in this scene, destroy the original object
						if (item.LoadType == ELoadType.Create)
							Object.DestroyImmediate(registeredObject.GetGameObject());
					}
				}
				
				// Don't load objects that don't belong in this scene
				if (string.IsNullOrEmpty(item.TransferredScene))
				{
					if (item.OriginalScene != currentSceneName)
						continue;
				}
				else
				{
					if (item.TransferredScene != currentSceneName)
						continue;
				}
				
				switch (item.LoadType)
				{
					case ELoadType.Create:
					{
						// Nothing to create if the object id is empty
						if (string.IsNullOrEmpty(objectID))
							continue;
						
						// Don't create if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(objectID) || data.KilledAlives.Contains(objectID))
							continue;

						// Make sure the create type assembly is for a mod that exists
						var split = item.CreateData.Item1.Split(", ");
						if (split.Length > 1)
						{
							if (!modNames.Contains(split[1]))
							{
								Debug.LogWarning($"[StateManager] Mod to Create type {item.CreateData.Item1} for saveable with ID {objectID} is missing");
								continue;
							}
						}
						
						var type = Type.GetType(item.CreateData.Item1);
						if (type == null)
						{
							Debug.LogWarning($"[StateManager] Failed to get Create type {item.CreateData.Item1} for saveable with ID {objectID}");
							continue;
						}

						var method = ReflectionTools.GetMethodDeep(type, "ApplyCreation", BindingFlags.Static | BindingFlags.Public, typeof(MonoBehaviour));
						if (method == null)
						{
							Debug.LogWarning($"[StateManager] Saveable with type {type} does not have ApplyCreation method");
							continue;
						}
						
						try
						{
							var result = method.Invoke(null, new object[] { new Tuple<string, JObject>(objectID, item.CreateData.Item2) });
							if (result is ISaveable saveable && !saveable.IsNull())
							{
								saveable.OriginalScene = item.OriginalScene;
								saveable.TransferredScene = item.TransferredScene;
							}
						}
						catch (Exception e)
						{
							Debug.LogError($"[StateManager] Failed creating saveable with type {type} ({objectID}), {e}");
						}
						
						break;
					}
					case ELoadType.Modify:
					{
						if (!saveables.TryGetValue(objectID, out var saveable))
						{
							Debug.LogWarning($"[StateManager] Modify saveable with ID {objectID} was not found");
							continue;
						}
						
						saveable.OriginalScene = item.OriginalScene;
						saveable.TransferredScene = item.TransferredScene;
						
						// No data for destroyed objects, just remove it
						if (data.DestroyedObjects.Contains(saveable.ObjectID))
						{
							Object.Destroy(saveable.GetGameObject());
							continue;
						}

						if (timing == ELoadTiming.Alives)
						{
							if (saveable is not IAlive alive)
							{
								Debug.LogWarning($"[StateManager] Saveable with ID {objectID} is not an IAlive");
								continue;
							}
						
							if (alive.IsNull() || !alive.IsAlive)
								continue;
							
							// No data for killed alives, just remove it
							if (data.KilledAlives.Contains(alive.ObjectID))
							{
								killAlives.Add(alive);
								continue;
							}
						}
						
						try
						{
							saveable.ApplyModifications(item.ModifyData);
						}
						catch (Exception e)
						{
							Debug.LogError($"[StateManager] Failed loading {saveable.GetType().Name} state for {TransformTools.GetFullPath(saveable.GetTransform())} ({saveable.ObjectID}), {e}");
						}
						
						break;
					}
				}
			}
		}

		#endregion
	}
}