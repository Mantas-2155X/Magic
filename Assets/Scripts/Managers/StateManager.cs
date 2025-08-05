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
using UnityEngine;
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

		private readonly Dictionary<string, SaveData> availableSaves = new ();

		public Dictionary<string, SaveData> GetSaves()
		{
			return availableSaves;
		}
		
		public void Save()
		{
			var sceneManager = SceneManager.Instance;
			
			var currentSceneData = sceneManager.GetCurrentSceneData();
			if (!currentSceneData.SupportsSaving)
			{
				Debug.LogError($"[StateManager] Not saving save data as the scene {currentSceneData.LocalizedName} does not support saving");
				return;
			}
			
			var data = new SaveData();
			data.Scene = currentSceneData.Name;
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
			
			data.Items = new List<SaveData.SaveItem>();
			
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
						ObjectID = saveable.ObjectID
					};

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
					
					data.Items.Add(item);
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
				return;
			}

			var modifications = 0;
			var creations = 0;

			for (var i = 0; i < data.Items.Count; i++)
			{
				var dataItem = data.Items[i];

				switch (dataItem.LoadType)
				{
					case ELoadType.Create:
						creations++;
						break;
					case ELoadType.Modify:
						modifications++;
						break;
				}
			}

			Debug.Log($"[StateManager] Save Statistics: Destroyed Components {data.DestroyedComponents.Count}, Destroyed Objects {data.DestroyedObjects.Count}, Items {data.Items.Count}, Creations {creations}, Modifications {modifications}, Killed Alives {data.KilledAlives.Count}");
			
			File.WriteAllText(System.IO.Path.Combine(Path, $"{data.SavedTime:yyyy_MM_dd_HH_mm_ss_fff}.json"), saveData);
			
			initializeSaves();
		}

		public void Load(SaveData data)
		{
			loadAsync(data).Forget();
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

				var data = JsonConvert.DeserializeObject<SaveData>(text);
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

			lastSaveData = null;
		}

		private async UniTaskVoid loadAsync(SaveData data)
		{
			var sceneManager = SceneManager.Instance;

			var sceneData = ObjectManager.Instance.GetData<SceneData>(data.Scene);
			if (!sceneData.SupportsSaving)
			{
				Debug.LogError($"[StateManager] Not loading save data as the scene {data.Scene} does not support saving");
				return;
			}

			lastSaveData = data;
			
			if (sceneData == sceneManager.GetCurrentSceneData())
				await sceneManager.ReloadSceneAsync(true, true, true);
			else
				await sceneManager.ChangeSceneAsync(sceneData, true, true, true);

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
				var mod = modInfos[i];
				if (mod == null || mod.Disabled)
					continue;

				modNames.AddUnique($"{mod.Author}.{mod.Name}");
			}
			
			for (var i = 0; i < data.Items.Count; i++)
			{
				var item = data.Items[i];
				if (item.LoadTiming != timing)
					continue;
				
				switch (item.LoadType)
				{
					case ELoadType.Create:
					{
						// Nothing to create if the object id is empty
						if (string.IsNullOrEmpty(item.ObjectID))
							continue;
						
						// Don't create if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(item.ObjectID) || data.KilledAlives.Contains(item.ObjectID))
							continue;

						// Make sure the create type assembly is for a mod that exists
						var split = item.CreateData.Item1.Split(", ");
						if (split.Length > 1)
						{
							if (!modNames.Contains(split[1]))
							{
								Debug.LogWarning($"[StateManager] Mod to Create type {item.CreateData.Item1} for saveable with ID {item.ObjectID} is missing");
								continue;
							}
						}
						
						var type = Type.GetType(item.CreateData.Item1);
						if (type == null)
						{
							Debug.LogWarning($"[StateManager] Failed to get Create type {item.CreateData.Item1} for saveable with ID {item.ObjectID}");
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
							method.Invoke(null, new object[] { new Tuple<string, JObject>(item.ObjectID, item.CreateData.Item2) });
						}
						catch (Exception e)
						{
							Debug.LogError($"[StateManager] Failed creating saveable with type {type} ({item.ObjectID}), {e}");
						}
						
						break;
					}
					case ELoadType.Modify:
					{
						if (!saveables.TryGetValue(item.ObjectID, out var saveable))
						{
							Debug.LogWarning($"[StateManager] Modify saveable with ID {item.ObjectID} was not found");
							continue;
						}
						
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
								Debug.LogWarning($"[StateManager] Saveable with ID {item.ObjectID} is not an IAlive");
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