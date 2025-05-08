using System;
using System.Collections.Generic;
using System.IO;
using AI;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Base;
using Objects.Interfaces;
using ScriptableObjects;
using State;
using State.Enums;
using UnityEngine;

namespace Managers
{
	// TODO (impl):
	// (Objects) BaseElevator, BaseConveyor
	// (AI) NPC, Mid-cast info
	// (Combat) Launched projectiles, Active attacks, Spells, Decals
	// (World) World7 Orb, World6 Waves, World4 Timer
	// Trigger, DelayedAttack, DelayedTrigger
	
	// TODO (test):
	// (Objects) DroppedWearable
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
			data.Objects = new Dictionary<string, Dictionary<string, JObject>>();
			data.Alives = new Dictionary<string, Dictionary<string, JObject>>();
			
			var world = World.World.Instance;

			var gibs = world.Ragdolls.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < gibs.Length; i++)
			{
				var component = gibs[i];
				if (component is not BaseGib gib)
					continue;
				
				// Leave gibs without ID as they are
				if (string.IsNullOrEmpty(gib.ObjectID))
					continue;
				
				// Don't save destroyed gibs data
				if (DestroyedObjects.Contains(gib.ObjectID))
					continue;
				
				// Keep data of destroyed components as it might hold stuff outside of the component and be used
				
				try
				{
					data.Create[gib.ObjectID] = new CreateData
					{
						Type = ECreateType.Gib,
						Name = gib.ObjectData.Name,
						States = gib.Save()
					};
				}
				catch (Exception e)
				{
					Debug.LogError($"[StateManager] Failed saving gib state for {component.name} ({gib.ObjectID}), {e}");
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
				
				// Don't save destroyed objects data
				if (DestroyedObjects.Contains(iObject.ObjectID))
					continue;
				
				// Keep data of destroyed components as it might hold stuff outside of the component and be used
				
				try
				{
					data.Objects[iObject.ObjectID] = iObject.Save();
				}
				catch (Exception e)
				{
					Debug.LogError($"[StateManager] Failed saving object state for {component.name} ({iObject.ObjectID}), {e}");
				}
			}

			foreach (var pair in AIManager.Instance.AlivesColliderMap)
			{
				var alive = pair.Value;
				if (alive == null || !alive.IsAlive)
					continue;
				
				// Leave alives without ID as they are
				if (string.IsNullOrEmpty(alive.ObjectID))
					continue;

				// Don't save killed alives data
				if (KilledAlives.Contains(alive.ObjectID))
					continue;

				if (alive is NPC npc && npc.ExternallySpawned)
				{
					try
					{
						data.Create[npc.ObjectID] = new CreateData
						{
							Type = ECreateType.NPC,
							Name = npc.Data.Name,
							States = npc.Save()
						};
					}
					catch (Exception e)
					{
						Debug.LogError($"[StateManager] Failed saving npc state for {npc.name} ({npc.ObjectID}), {e}");
					}
				}
				else
				{
					try
					{
						data.Alives[alive.ObjectID] = alive.Save();
					}
					catch (Exception e)
					{
						Debug.LogError($"[StateManager] Failed saving alive state for {alive.GetGameObject().name} ({alive.ObjectID}), {e}");
					}
				}
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

			foreach (var pair in data.Create)
			{
				if (string.IsNullOrEmpty(pair.Key))
					continue;

				IObject iObject = null;
				IAlive iAlive = null;

				var type = pair.Value.Type;

				switch (type)
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
						}
						catch(Exception e)
						{
							Debug.LogError($"[StateManager] Failed loading gib state for {gib.name} ({gib.ObjectID}), {e}");
						}

						iObject = gib;
						break;
					}
					case ECreateType.NPC:
						// Don't create a npc if it's supposed to be killed already
						if (data.KilledAlives.Contains(pair.Key))
							continue;
						
						var npc = AIManager.Instance.CreateNPC(Vector3.zero, Vector3.zero, (NPCData)objectManager.GetAlive(pair.Value.Name));
						npc.ObjectID = pair.Key;
						
						try
						{
							npc.Load(pair.Value.States);
						}
						catch(Exception e)
						{
							Debug.LogError($"[StateManager] Failed loading npc state for {npc.name} ({npc.ObjectID}), {e}");
						}
						
						iAlive = npc;
						break;
				}

				if (iObject != null)
				{
					// Other potentially needed data is set so we can remove the component now
					if (data.DestroyedComponents.Contains(iObject.ObjectID))
					{
						UnityEngine.Object.Destroy((Component)iObject);
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
					UnityEngine.Object.Destroy(iObject.GetGameObject());
					continue;
				}
				
				if (data.Objects.TryGetValue(iObject.ObjectID, out var objectState))
				{
					try
					{
						iObject.Load(objectState);
					}
					catch (Exception e)
					{
						Debug.LogError($"[StateManager] Failed loading object state for {component.name} ({iObject.ObjectID}), {e}");
					}
				}

				// Other potentially needed data is set so we can remove the component now
				if (data.DestroyedComponents.Contains(iObject.ObjectID))
				{
					UnityEngine.Object.Destroy((Component)iObject);
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
					try
					{
						alive.Load(aliveState);
					}
					catch (Exception e)
					{
						Debug.LogError($"[StateManager] Failed loading alive state for {alive.GetGameObject().name} ({alive.ObjectID}), {e}");
					}
				}
			}

			for (var i = killAlives.Count - 1; i >= 0; i--)
				killAlives[i].Kill(null, true);
		}
	}
}