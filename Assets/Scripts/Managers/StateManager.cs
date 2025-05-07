using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using State;
using UnityEngine;

namespace Managers
{
	// TODO (impl):
	// (Objects) BaseElevator, BaseConveyor, NPCSpawner
	// (AI) NPC, Slow/Paralyze info, Mid-cast info
	// (Combat) Launched projectiles, Active attacks, Spells, Decals
	// (World) World7 Orb, World6 Waves, World4 Timer
	// Trigger, Notify, DelayedAttack, DelayedTrigger
	
	// TODO (test):
	// (Objects) DroppedWearable
	// (AI) Grabbing shrink
	
	public class StateManager : MonoBehaviour
	{
		public static StateManager Instance;

		public const int Version = 1;
		
		[SerializeField]
		public List<string> DestroyedObjects = new ();
		
		[SerializeField]
		public List<string> DestroyedComponents = new ();

		[SerializeField]
		public List<string> KilledAlives = new ();
		
		public void Awake()
		{
			Instance = this;
		}

		public void Save()
		{
			var data = new SaveData();
			data.FileVersion = Version;
			data.Scene = SceneManager.Instance.GetCurrentScene();
			data.DestroyedObjects = DestroyedObjects;
			data.DestroyedComponents = DestroyedComponents;
			data.KilledAlives = KilledAlives;
			data.Objects = new Dictionary<string, Dictionary<string, JObject>>();
			data.Alives = new Dictionary<string, Dictionary<string, JObject>>();
			
			var world = World.World.Instance;

			var components = world.Objects.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
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

				// Don't save destroyed alives data
				if (KilledAlives.Contains(alive.ObjectID))
					continue;

				try
				{
					data.Alives[alive.ObjectID] = alive.Save();
				}
				catch (Exception e)
				{
					Debug.LogError($"[StateManager] Failed saving alive state for {alive.GetGameObject().name} ({alive.ObjectID}), {e}");
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

			await SceneManager.Instance.ReloadSceneAsync(true, true, true);
			
			await UniTask.NextFrame();
			await UniTask.NextFrame();
			await UniTask.NextFrame();
			
			var world = World.World.Instance;

			var components = world.Objects.GetComponentsInChildren<Component>(true);
			for (var i = 0; i < components.Length; i++)
			{
				var component = components[i];
				if (component is not IObject iObject)
					continue;
				
				// Leave objects without ID as they are
				if (string.IsNullOrEmpty(iObject.ObjectID))
					continue;

				// No data for destroyed objects, just remove it
				if (data.DestroyedObjects.Contains(iObject.ObjectID))
				{
					Destroy(iObject.GetGameObject());
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
					Destroy((Component)iObject);
					continue;
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

				// No data for killed alives, just remove it
				if (data.KilledAlives.Contains(alive.ObjectID))
				{
					alive.Kill(null);
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

			var stateManager = Instance;
			
			stateManager.DestroyedObjects = data.DestroyedObjects;
			stateManager.DestroyedComponents = data.DestroyedComponents;
			
			stateManager.KilledAlives = data.KilledAlives;
		}
	}
}