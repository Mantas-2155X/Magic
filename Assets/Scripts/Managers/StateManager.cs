using System;
using System.Collections.Generic;
using System.IO;
using AI;
using AI.Interfaces;
using Components;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects;
using Objects.Base;
using Objects.Interfaces;
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
	// (AI) NPC
	// (Combat) Launched projectiles, Active attacks, Decals
	// (World) World7 Orb, World6 Waves, World4 Timer
	// (Component) DelayedAttack, DelayedTrigger
	
	// TODO (test):
	// (AI) Grabbing shrink
	
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
				return instance;
			}
		}

		public const int Version = 1;
		
		public List<string> DestroyedObjects = new ();
		public List<string> DestroyedComponents = new ();
		public List<string> KilledAlives = new ();
		
		private SaveData lastSaveData;

		public void Reinitialize()
		{
			DestroyedObjects.Clear();
			DestroyedComponents.Clear();
			KilledAlives.Clear();

			if (lastSaveData == null)
				return;

			DestroyedObjects = lastSaveData.DestroyedObjects;
			DestroyedComponents = lastSaveData.DestroyedComponents;
			KilledAlives = lastSaveData.KilledAlives;

			lastSaveData = null;
		}

		public void Save()
		{
			var data = new SaveData();
			data.FileVersion = Version;
			data.Scene = SceneManager.Instance.GetCurrentScene();
			data.DestroyedObjects = DestroyedObjects;
			data.DestroyedComponents = DestroyedComponents;
			data.KilledAlives = KilledAlives;
			data.Create = new Dictionary<string, CreateData>();
			data.World = new Dictionary<string, WorldData>();
			data.Objects = new Dictionary<string, Dictionary<string, JObject>>();
			data.Alives = new Dictionary<string, Dictionary<string, JObject>>();
			
			var world = World.World.Instance;
			var worldTr = world.transform;

			var components = Object.FindObjectsByType<Component>(FindObjectsInactive.Include, FindObjectsSortMode.None);
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
				if (component is not ISaveable saveable)
					continue;
				
				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} has no Object ID, skipping");
					continue;
				}

				// Make sure there is a root transform
				var root = component.transform.root;
				if (root == null)
				{
					Debug.LogWarning($"[StateManager] Saveable {saveable.GetType().Name} on {TransformTools.GetFullPath(component.transform)} does not have a root, skipping");
					continue;
				}

				var saved = false;
				
				try
				{
					if (root == worldTr)
					{
						if (saveable is Trigger trigger)
						{
							data.World[trigger.ObjectID] = new WorldData
							{
								Type = EWorldDataType.Trigger,
								States = trigger.Save()
							};
							saved = true;
						}
					}
					else
					{
						// Don't save destroyed data
						if (DestroyedObjects.Contains(saveable.ObjectID))
							continue;

						// Keep data of destroyed components as it might hold stuff outside of the component and be used
						
						if (root == world.Ragdolls)
						{
							if (saveable is BaseGib gib)
							{
								data.Create[gib.ObjectID] = new CreateData
								{
									Type = ECreateType.Gib,
									Name = gib.ObjectData.Name,
									States = gib.Save()
								};
								saved = true;
							}
						}
						else if (root == world.Objects)
						{
							if (saveable is IObject iObject)
							{
								if (saveable is DroppedWearable droppedWearable)
								{
									data.Create[droppedWearable.ObjectID] = new CreateData
									{
										Type = ECreateType.DroppedWearable,
										Name = droppedWearable.Wearable.WearableData.Name, // Actual wearable name here instead of droppedwearable
										States = droppedWearable.Save()
									};
								}
								else
								{
									data.Objects[iObject.ObjectID] = iObject.Save();
								}
								
								saved = true;
							}
						}
						else if (root == world.Characters)
						{
							if (saveable is IAlive iAlive)
							{
								// Don't save killed alives data
								if (!iAlive.IsAlive || KilledAlives.Contains(iAlive.ObjectID))
									continue;

								if (iAlive is NPC npc && npc.ExternallySpawned)
								{
									data.Create[npc.ObjectID] = new CreateData
									{
										Type = ECreateType.NPC,
										Name = npc.Data.Name,
										States = npc.Save()
									};
								}
								else
								{
									data.Alives[iAlive.ObjectID] = iAlive.Save();
								}
								
								saved = true;
							}
						}
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

			File.WriteAllText("save.json", JsonConvert.SerializeObject(data, Formatting.Indented));
		}

		public void Load()
		{
			loadAsync().Forget();
		}

		private async UniTaskVoid loadAsync()
		{
			if (!File.Exists("save.json"))
				return;

			var currentScene = SceneManager.Instance.GetCurrentScene();

			var data = JsonConvert.DeserializeObject<SaveData>(await File.ReadAllTextAsync("save.json"));
			if (data.Scene != SceneManager.Instance.GetCurrentScene())
			{
				Debug.LogError($"[StateManager] Not loading save as the scene is incorrect. Expecting {data.Scene} while currently {currentScene}");
				return;
			}

			lastSaveData = data;
			
			await SceneManager.Instance.ReloadSceneAsync(true, true, true);
			
			var world = World.World.Instance;
			var objectManager = ObjectManager.Instance;

			var worldComponents = world.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < worldComponents.Length; i++)
			{
				var component = worldComponents[i];
				if (component is not ISaveable saveable)
					continue;

				// Leave saveables without ID as they are
				if (string.IsNullOrEmpty(saveable.ObjectID))
					continue;
					
				if (data.World.TryGetValue(saveable.ObjectID, out var worldData))
				{
					bool loaded;

					try
					{
						// Make sure to load correct type
						switch (worldData.Type)
						{
							case EWorldDataType.Trigger:
							{
								if (saveable is not Trigger)
									continue;
								break;
							}
						}
						
						saveable.Load(worldData.States);
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
			
			foreach (var pair in data.Create)
			{
				// Nothing to create if the name is empty
				if (string.IsNullOrEmpty(pair.Key))
					continue;

				IObject iObject = null;
				IAlive iAlive = null;

				var loaded = false;

				switch (pair.Value.Type)
				{
					case ECreateType.Gib:
					{
						// Don't create a gib if it's supposed to be destroyed already
						if (data.DestroyedObjects.Contains(pair.Key))
							continue;
						
						var gib = (BaseGib)objectManager.CreateObject(objectManager.GetObject(pair.Value.Name), Vector3.zero, Vector3.zero);
						gib.ObjectID = pair.Key;

						var gibTr = gib.GetTransform();
						gibTr.SetParent(world.Ragdolls);

						try
						{
							gib.Load(pair.Value.States);
							loaded = true;
						}
						catch(Exception e)
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

						var npc = AIManager.Instance.CreateNPC(Vector3.zero, Vector3.zero, (NPCData)objectManager.GetAlive(pair.Value.Name));
						npc.ObjectID = pair.Key;

						try
						{
							npc.Load(pair.Value.States);
							loaded = true;
						}
						catch(Exception e)
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
						
						var wearable = objectManager.CreateWearable(objectManager.GetWearable(pair.Value.Name), Vector3.zero, Vector3.zero);
						wearable.ObjectID = pair.Key;
						wearable.Drop();

						var droppedWearable = wearable.GetGameObject().GetComponent<DroppedWearable>();

						try
						{
							droppedWearable.Load(pair.Value.States);
							loaded = true;
						}
						catch(Exception e)
						{
							loaded = false;
							Debug.LogError($"[StateManager] Failed loading created dropped wearable state for {droppedWearable.name} ({droppedWearable.ObjectID}), {e}");
						}

						iObject = droppedWearable;
						break;
					}
				}
				
				if (!loaded)
					Debug.LogWarning($"[StateManager] Create Saveable {pair.Value.Type} with ObjectID {pair.Key} was not loaded");

				if (iObject != null)
				{
					// Other potentially needed data is set so we can remove the component now
					if (data.DestroyedComponents.Contains(iObject.ObjectID))
					{
						Object.Destroy((Component)iObject);
						continue;
					}
				}

				if (iAlive != null)
				{
					// 
				}
			}
			
			var objects = world.Objects.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < objects.Length; i++)
			{
				var component = objects[i];
				if (component is not IObject iObject)
					continue;
				
				// Leave objects without ID as they are
				if (string.IsNullOrEmpty(iObject.ObjectID))
					continue;

				// No data for destroyed objects, just remove it
				if (data.DestroyedObjects.Contains(iObject.ObjectID))
				{
					Object.Destroy(iObject.GetGameObject());
					continue;
				}
				
				if (data.Objects.TryGetValue(iObject.ObjectID, out var objectState))
				{
					bool loaded;

					try
					{
						iObject.Load(objectState);
						loaded = true;
					}
					catch (Exception e)
					{
						loaded = false;
						Debug.LogError($"[StateManager] Failed loading object state for {TransformTools.GetFullPath(component.transform)} ({iObject.ObjectID}), {e}");
					}
					
					if (!loaded)
						Debug.LogWarning($"[StateManager] Object Saveable {iObject.GetType().Name} on {TransformTools.GetFullPath(component.transform)} was not loaded");
				}

				// Other potentially needed data is set so we can remove the component now
				if (data.DestroyedComponents.Contains(iObject.ObjectID))
				{
					Object.Destroy((Component)iObject);
					continue;
				}
			}

			var killAlives = new List<IAlive>();
			
			foreach (var pair in AIManager.Instance.AlivesColliderMap)
			{
				var alive = pair.Value;
				if (alive == null || !alive.IsAlive)
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

			for (var i = killAlives.Count - 1; i >= 0; i--)
				killAlives[i].Kill(null, true);
		}
	}
}