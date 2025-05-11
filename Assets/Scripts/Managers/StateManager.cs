using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AI;
using AI.Interfaces;
using Combat.Decals.Interfaces;
using Combat.Projectiles.Interfaces;
using Components;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects;
using Objects.Base;
using Objects.Interfaces;
using Scenes;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using Tools;
using UnityEngine;
using World;
using Object = UnityEngine.Object;

namespace Managers
{
	// TODO (impl):
	// (Objects) BaseElevator, BaseConveyor
	// (Combat) Attacks
	// (World) World6 Waves, World4 Timer
	// (AI) Switch cast cooldown, Chase interrupt timer, Off mesh link travelling
	
	// TODO (other):
	// Figure out how to simulate particle systems correctly
	// Used in a bunch of places like world7, projectiles, attacks etc
	
	// TODO (test):
	// (AI) Grabbing shrink, Patrol Already Waited, BaseDoor (with button)
	
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

			Debug.LogWarning($"[StateManager] No registered object with ID {objectID} found");
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
			var currentScene = sceneManager.GetCurrentScene();
			
			if (!currentSceneData.SupportsSaving)
			{
				Debug.LogError($"[StateManager] Not saving save data as the scene {currentScene} does not support saving");
				return;
			}
			
			var data = new SaveData();
			data.Scene = currentScene;
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
			
			data.Create = new Dictionary<string, JObject>();
			data.DeferredCreate = new Dictionary<string, JObject>();
			data.VeryDeferredCreate = new Dictionary<string, JObject>();
			
			data.World = new Dictionary<string, Dictionary<string, JObject>>();
			data.DeferredWorld = new Dictionary<string, Dictionary<string, JObject>>();
			data.VeryDeferredWorld = new Dictionary<string, Dictionary<string, JObject>>();
			
			data.Objects = new Dictionary<string, Dictionary<string, JObject>>();
			data.DeferredObjects = new Dictionary<string, Dictionary<string, JObject>>();
			data.VeryDeferredObjects = new Dictionary<string, Dictionary<string, JObject>>();
			
			data.Alives = new Dictionary<string, Dictionary<string, JObject>>();
			
			var world = World.World.Instance;
			var worldTr = world.transform;

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

				// Make sure there is a root transform
				var root = component.transform.root;
				if (root == null)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} does not have a root, skipping");
					continue;
				}
				
				// Prevent saving stuff like gibs on characters
				if (root == world.Characters && saveable is not IAlive and not IDecal)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} is not saved on characters, skipping");
					continue;
				}
				
				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} has no Object ID, skipping");
					continue;
				}

				var saved = false;
				
				try
				{
					if (root == worldTr)
					{
						if (saveable is World7)
						{
							data.World[saveable.ObjectID] = saveable.Save();
							saved = true;
						}
						else if (saveable is Trigger or DelayedTrigger or Water)
						{
							data.DeferredWorld[saveable.ObjectID] = saveable.Save();
							saved = true;
						}
					}
					else
					{
						// Don't save destroyed data
						if (destroyedObjects.Contains(saveable.ObjectID))
							continue;

						// Keep data of destroyed components as it might hold stuff outside of the component and be used
						
						if (root == world.Ragdolls)
						{
							if (saveable is BaseGib gib)
							{
								var createData = new CreateData
								{
									Type = ECreateType.Gib,
									Name = gib.ObjectData.Name,
									States = gib.Save()
								};
								
								data.Create[gib.ObjectID] = JObject.FromObject(createData);
								saved = true;
							}
						}
						else if (root == world.Projectiles)
						{
							if (saveable is IProjectile projectile)
							{
								var projectileCreateData = new ProjectileCreateData
								{
									Type = ECreateType.Projectile,
									Name = projectile.ProjectileData.Name,
									Range = projectile.SpellRange,
									Attack = projectile.AttackData != null ? projectile.AttackData.Name : null,
									SourceObjectID = projectile.Source.NotNull() ? projectile.Source.ObjectID : null,
									ElapsedTime = Time.time - projectile.CreatedTime,
									States = projectile.Save()
								};
								
								data.DeferredCreate[projectile.ObjectID] = JObject.FromObject(projectileCreateData);
								saved = true;
							}
						}
						else if (root == world.Objects)
						{
							if (saveable is IObject)
							{
								if (saveable is DroppedWearable droppedWearable)
								{
									var createData = new CreateData
									{
										Type = ECreateType.DroppedWearable,
										Name = droppedWearable.Wearable.WearableData.Name, // Actual wearable name here instead of droppedwearable
										States = droppedWearable.Save()
									};
									
									data.Create[droppedWearable.ObjectID] = JObject.FromObject(createData);
								}
								else
								{
									data.Objects[saveable.ObjectID] = saveable.Save();
								}
								
								saved = true;
							}
							else if (saveable is Trigger or DelayedTrigger)
							{
								data.DeferredObjects[saveable.ObjectID] = saveable.Save();
								saved = true;
							}
						}
						else if (root == world.Characters)
						{
							if (saveable is IAlive iAlive)
							{
								// Don't save killed alives data
								if (!iAlive.IsAlive || killedAlives.Contains(iAlive.ObjectID))
									continue;

								if (iAlive is NPC npc && npc.ExternallySpawned)
								{
									var createData = new CreateData
									{
										Type = ECreateType.NPC,
										Name = npc.Data.Name,
										States = npc.Save()
									};
									
									data.Create[npc.ObjectID] = JObject.FromObject(createData);
								}
								else
								{
									data.Alives[iAlive.ObjectID] = iAlive.Save();
								}
								
								saved = true;
							}
						}
					}
					
					if (saveable is IDecal decal)
					{
						var decalCreateData = new DecalCreateData
						{
							Type = ECreateType.Decal,
							Name = decal.DecalData.Name,
							AttachObjectID = decal.Attach.NotNull() ? decal.Attach.ObjectID : null,
							NormalizedTime = decal.NormalizedTime,
							ElapsedTime = Time.time - decal.CreatedTime,
							States = decal.Save()
						};
								
						data.VeryDeferredCreate[decal.ObjectID] = JObject.FromObject(decalCreateData);
						saved = true;
					}
				}
				catch (Exception e)
				{
					saved = false;
					Debug.LogError($"[StateManager] Failed saving {saveable.GetType().Name} state for {TransformTools.GetFullPath(component.transform)} ({saveable.ObjectID}), {e}");
				}

				if (!saved)
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} was not saved");
			}

			var saveData = JsonConvert.SerializeObject(data, Formatting.Indented);
			if (string.IsNullOrEmpty(saveData))
			{
				Debug.LogError("[StateManager] Not saving save data as the object failed to serialize");
				return;
			}

			File.WriteAllText(System.IO.Path.Combine(Path, $"{currentScene}_{data.SavedTime:yyyy_MM_dd_HH_mm_ss_fff}.json"), saveData);
			
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

		private void onPreSceneLoad(string scene)
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

			var sceneData = ObjectManager.Instance.GetScene($"SCENE_{data.Scene.ToUpper()}_NAME");
			if (!sceneData.SupportsSaving)
			{
				Debug.LogError($"[StateManager] Not loading save data as the scene {data.Scene} does not support saving");
				return;
			}

			lastSaveData = data;
			
			if (data.Scene == sceneManager.GetCurrentScene())
				await sceneManager.ReloadSceneAsync(true, true, true);
			else
				await sceneManager.ChangeSceneAsync(data.Scene, true, true, true);
			
			var killAlives = new List<IAlive>();
			
			loadWorld(data, EDefer.Normal);
			loadCreate(data, EDefer.Normal);
			loadObjects(data, EDefer.Normal);

			loadAlives(data, killAlives);
				
			loadWorld(data, EDefer.Deferred);
			loadCreate(data, EDefer.Deferred);
			loadObjects(data, EDefer.Deferred);

			loadWorld(data, EDefer.VeryDeferred);
			loadCreate(data, EDefer.VeryDeferred);
			loadObjects(data, EDefer.VeryDeferred);

			for (var i = killAlives.Count - 1; i >= 0; i--)
				killAlives[i].Kill(null, true);
		}
		
		private void loadWorld(SaveData data, EDefer defer)
		{
			var world = World.World.Instance;

			Dictionary<string, Dictionary<string, JObject>> dict;

			switch (defer)
			{
				case EDefer.Normal:
					dict = data.World;
					break;
				case EDefer.Deferred:
					dict = data.DeferredWorld;
					break;
				case EDefer.VeryDeferred:
					dict = data.VeryDeferredWorld;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(defer), defer, null);
			}
			
			var worldComponents = world.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < worldComponents.Length; i++)
			{
				var component = worldComponents[i];
				if (component is not ISaveable saveable)
					continue;

				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
					continue;
					
				if (dict.TryGetValue(saveable.ObjectID, out var worldState))
				{
					bool loaded;

					try
					{
						saveable.Load(worldState);
						loaded = true;
					}
					catch (Exception e)
					{
						loaded = false;
						Debug.LogError($"[StateManager] Failed loading {saveable.GetType().Name} state for {TransformTools.GetFullPath(component.transform)} ({saveable.ObjectID}), {e}");
					}
					
					if (!loaded)
						Debug.LogWarning($"[StateManager] World Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} was not loaded");
				}
			}
		}

		private void loadCreate(SaveData data, EDefer defer)
		{
			var world = World.World.Instance;
			var objectManager = ObjectManager.Instance;

			Dictionary<string, JObject> dict;

			switch (defer)
			{
				case EDefer.Normal:
					dict = data.Create;
					break;
				case EDefer.Deferred:
					dict = data.DeferredCreate;
					break;
				case EDefer.VeryDeferred:
					dict = data.VeryDeferredCreate;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(defer), defer, null);
			}

			foreach (var pair in dict)
			{
				// Nothing to create if the name is empty
				if (string.IsNullOrEmpty(pair.Key))
					continue;

				IObject iObject = null;
				IAlive iAlive = null;
				IProjectile iProjectile = null;
				IDecal iDecal = null;

				var loaded = false;

				var createDataJObject = pair.Value;
				var createData = createDataJObject.ToObject<CreateData>();
				
				switch (createData.Type)
				{
					case ECreateType.Gib:
					{
						// Don't create a gib if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(pair.Key))
							continue;
						
						var gib = (BaseGib)objectManager.CreateObject(objectManager.GetObject(createData.Name), Vector3.zero, Vector3.zero);
						gib.ObjectID = pair.Key;

						var gibTr = gib.GetTransform();
						gibTr.SetParent(world.Ragdolls);

						try
						{
							gib.Load(createData.States);
							loaded = true;
						}
						catch (Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created gib state for {gib.name} ({gib.ObjectID}), {e}");
						}

						iObject = gib;
						break;
					}
					case ECreateType.NPC:
					{
						// Don't create a npc if it's supposed to be killed already
						if (data.KilledAlives.Contains(pair.Key))
							continue;

						var npc = AIManager.Instance.CreateNPC(Vector3.zero, Vector3.zero, (NPCData)objectManager.GetAlive(createData.Name));
						npc.ObjectID = pair.Key;

						try
						{
							npc.Load(createData.States);
							loaded = true;
						}
						catch (Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created npc state for {npc.name} ({npc.ObjectID}), {e}");
						}

						iAlive = npc;
						break;
					}
					case ECreateType.DroppedWearable:
					{
						// Don't create a dropped wearable if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(pair.Key))
							continue;
						
						var wearable = objectManager.CreateWearable(objectManager.GetWearable(createData.Name), Vector3.zero, Vector3.zero);
						wearable.ObjectID = pair.Key;
						wearable.Drop();

						var droppedWearable = wearable.GetGameObject().GetComponent<DroppedWearable>();

						try
						{
							droppedWearable.Load(createData.States);
							loaded = true;
						}
						catch (Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created dropped wearable state for {droppedWearable.name} ({droppedWearable.ObjectID}), {e}");
						}

						iObject = droppedWearable;
						break;
					}
					case ECreateType.Projectile:
					{
						// Don't create a projectile if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(pair.Key))
							continue;

						var projectileCreateData = createDataJObject.ToObject<ProjectileCreateData>();
						
						var projectile = objectManager.CreateProjectile(objectManager.GetProjectile(projectileCreateData.Name), projectileCreateData.Range, objectManager.GetAttack(projectileCreateData.Attack), GetRegisteredObject(projectileCreateData.SourceObjectID), Vector3.zero, Vector3.zero, projectileCreateData.ElapsedTime);
						projectile.ObjectID = pair.Key;

						try
						{
							projectile.Load(projectileCreateData.States);
							loaded = true;
						}
						catch (Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created projectile state for {((Component)projectile).name} ({projectile.ObjectID}), {e}");
						}

						iProjectile = projectile;
						break;
					}
					case ECreateType.Decal:
					{
						// Don't create a decal if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(pair.Key))
							continue;

						var decalCreateData = createDataJObject.ToObject<DecalCreateData>();
						
						var decal = objectManager.CreateDecal(objectManager.GetDecal(decalCreateData.Name), Vector3.zero, Quaternion.identity, GetRegisteredObject(decalCreateData.AttachObjectID), decalCreateData.ElapsedTime, decalCreateData.NormalizedTime);
						decal.ObjectID = pair.Key;

						try
						{
							decal.Load(decalCreateData.States);
							loaded = true;
						}
						catch (Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created decal state for {((Component)decal).name} ({decal.ObjectID}), {e}");
						}

						iDecal = decal;
						break;
					}
				}
				
				if (!loaded)
					Debug.LogWarning($"[StateManager] Create Saveable {createData.Type} with ObjectID {pair.Key} was not loaded");

				if (iObject.NotNull())
				{
					// Other potentially needed data is set so we can remove the component now
					if (data.DestroyedComponents.Contains(iObject.ObjectID))
					{
						Object.Destroy((Component)iObject);
						continue;
					}
				}

				if (iAlive.NotNull())
				{
					// 
				}
				
				if (iProjectile.NotNull())
				{
					// 
				}
				
				if (iDecal.NotNull())
				{
					// 
				}
			}
		}

		private void loadObjects(SaveData data, EDefer defer)
		{
			var world = World.World.Instance;
			
			Dictionary<string, Dictionary<string, JObject>> dict;

			switch (defer)
			{
				case EDefer.Normal:
					dict = data.Objects;
					break;
				case EDefer.Deferred:
					dict = data.DeferredObjects;
					break;
				case EDefer.VeryDeferred:
					dict = data.VeryDeferredObjects;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(defer), defer, null);
			}

			var objects = world.Objects.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < objects.Length; i++)
			{
				var component = objects[i];
				if (component is not ISaveable saveable)
					continue;
				
				// Leave objects without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
					continue;

				// No data for destroyed objects, just remove it
				if (data.DestroyedObjects.Contains(saveable.ObjectID))
				{
					Object.Destroy(saveable.GetGameObject());
					continue;
				}
				
				if (dict.TryGetValue(saveable.ObjectID, out var objectState))
				{
					bool loaded;

					try
					{
						saveable.Load(objectState);
						loaded = true;
					}
					catch (Exception e)
					{
						loaded = false;
						Debug.LogError($"[StateManager] Failed loading object state for {TransformTools.GetFullPath(component.transform)} ({saveable.ObjectID}), {e}");
					}
					
					if (!loaded)
						Debug.LogWarning($"[StateManager] Object Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} was not loaded");
				}

				// Other potentially needed data is set so we can remove the component now
				if (data.DestroyedComponents.Contains(saveable.ObjectID))
				{
					Object.Destroy((Component)saveable);
					continue;
				}
			}
		}

		private void loadAlives(SaveData data, List<IAlive> killAlives)
		{
			var currentAlives = AIManager.Instance.AlivesColliderMap.Values.ToList();
			for (var i = currentAlives.Count - 1; i >= 0; i--)
			{
				var alive = currentAlives[i];
				if (alive.IsNull() || !alive.IsAlive)
					continue;

				// Leave alives without ID as they are
				if (string.IsNullOrEmpty(alive.ObjectID))
					continue;

				// No data for killed alives, just remove it
				if (data.KilledAlives.Contains(alive.ObjectID))
				{
					killAlives.Add(alive);
					continue;
				}
				
				if (data.Alives.TryGetValue(alive.ObjectID, out var aliveState))
				{
					bool loaded;

					try
					{
						alive.Load(aliveState);
						loaded = true;
					}
					catch (Exception e)
					{
						loaded = false;
						Debug.LogError($"[StateManager] Failed loading alive state for {TransformTools.GetFullPath(alive.GetTransform())} ({alive.ObjectID}), {e}");
					}
					
					if (!loaded)
						Debug.LogWarning($"[StateManager] Alive Saveable {alive.GetType().Name} on {TransformTools.GetFullPath(alive.GetTransform())} was not loaded");
				}
			}
		}

		#endregion
	}
}