using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using State;
using UnityEngine;

namespace Managers
{
	public class StateManager : MonoBehaviour
	{
		public static StateManager Instance;

		public const int Version = 1;
		
		[SerializeField]
		public List<string> DestroyedObjects = new ();
		
		[SerializeField]
		public List<string> DestroyedComponents = new ();

		public void Awake()
		{
			Instance = this;
		}

		public void Save()
		{
			var data = new SaveData();
			data.FileVersion = Version;
			data.Scene = SceneManager.Instance.GetCurrentScene();
			data.Objects = new Dictionary<string, Dictionary<Type, JObject>>();
			data.DestroyedObjects = DestroyedObjects;
			data.DestroyedComponents = DestroyedComponents;
			
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

			File.WriteAllText("save.json", JsonConvert.SerializeObject(data, Formatting.Indented));
		}

		public void Load()
		{
			if (!File.Exists("save.json"))
				return;

			var currentScene = SceneManager.Instance.GetCurrentScene();

			var data = JsonConvert.DeserializeObject<SaveData>(File.ReadAllText("save.json"));
			if (data.Scene != SceneManager.Instance.GetCurrentScene())
			{
				Debug.LogError($"[StateManager] Not loading save as the scene is incorrect. Expecting {data.Scene} while currently {currentScene}");
				return;
			}

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
		}
	}
}