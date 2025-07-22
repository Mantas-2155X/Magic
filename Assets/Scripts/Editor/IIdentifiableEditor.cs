using System;
using System.Collections.Generic;
using System.IO;
using State.Interfaces;
using Tools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Editor
{
	public static class IIDentifiableEditor
	{
		[MenuItem("CONTEXT/Object/Generate ObjectID")]
		public static void GenerateObjectID(MenuCommand command) 
		{
			if (command.context is not IIdentifiable identifiable || identifiable.IsNull())
				return;
			
			identifiable.ObjectID = Guid.NewGuid().ToString();
			EditorUtility.SetDirty((Component)identifiable);
		}
		
		[MenuItem("CONTEXT/Object/Generate Selected ObjectIDs")]
		public static void GenerateSelectedObjectIDs(MenuCommand command) 
		{
			var gameObjects = Selection.gameObjects;
			for (var i = 0; i < gameObjects.Length; i++)
			{
				var gameObject = gameObjects[i];
				
				var identifiables = gameObject.GetComponents<IIdentifiable>();
				for (var k = 0; k < identifiables.Length; k++)
				{
					var identifiable = identifiables[k];
					identifiable.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty((Component)identifiable);
				}
			}
		}

		[MenuItem("CONTEXT/Object/Find Wrong ObjectIDs")]
		public static void FindWrongObjectIDs(MenuCommand command)
		{
			var objectIDs = new Dictionary<string, List<Tuple<string, string>>>();
			
			var sceneNames = Directory.GetFiles("Assets/Scenes", "*.unity", SearchOption.TopDirectoryOnly);
			for (var i = 0; i < sceneNames.Length; i++)
			{
				var fileInfo = new FileInfo(sceneNames[i]);
				if (!fileInfo.Exists)
					continue;

				try
				{
					var scene = SceneManager.GetSceneByPath(fileInfo.FullName);
				
					var shouldClose = true;
				
					if (!scene.isLoaded)
						scene = EditorSceneManager.OpenScene(fileInfo.FullName, OpenSceneMode.Additive);
					else
						shouldClose = false;
					
					objectIDs.Add(scene.name, new List<Tuple<string, string>>());
					
					var rootObjects = scene.GetRootGameObjects();
					for (var k = 0; k < rootObjects.Length; k++)
					{
						var identifiables = rootObjects[k].GetComponentsInChildren<IIdentifiable>();
						for (var l = 0; l < identifiables.Length; l++)
						{
							var identifiable = identifiables[l];
							objectIDs[scene.name].Add(new Tuple<string, string>(identifiable.ObjectID, TransformTools.GetFullPath(((Component)identifiable).transform)));
						}
					}
					
					if (shouldClose)
						EditorSceneManager.CloseScene(scene, true);
				}
				catch (Exception e)
				{
					Debug.LogError(e);
				}
			}

			foreach (var outerPair in objectIDs)
			{
				var outerScene = outerPair.Key;
				for (var i = 0; i < outerPair.Value.Count; i++)
				{
					var outerObjectID = outerPair.Value[i].Item1;
					
					if (string.IsNullOrWhiteSpace(outerObjectID))
					{
						Debug.Log($"Empty ObjectID at {outerScene} ({outerPair.Value[i].Item2})");
						continue;
					}
					
					foreach (var innerPair in objectIDs)
					{
						var scene = innerPair.Key;
						for (var k = 0; k < innerPair.Value.Count; k++)
						{
							var objectID = innerPair.Value[k].Item1;
							
							if (string.IsNullOrWhiteSpace(objectID))
								continue;
							
							if (i == k && outerScene == scene)
								continue;
							
							if (outerObjectID != objectID)
								continue;

							Debug.Log($"Duplicate ObjectID {objectID} at {scene} ({innerPair.Value[k].Item2}) and {outerScene} ({outerPair.Value[i].Item2})");
						}
					}
				}
			}
		}
	}
}