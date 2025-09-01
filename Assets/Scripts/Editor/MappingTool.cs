using System;
using System.Collections.Generic;
using Objects;
using Objects.Base;
using Objects.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	public class MappingTool : EditorWindow
	{
		[SerializeField]
		public bool ShowCenterOfMass = true;

		[SerializeField]
		public float CenterOfMassSize = 0.15f;
		
		private readonly Dictionary<Type, Dictionary<string, Tuple<Vector3, Vector3, Vector3>>> transforms = new ();
		
		[MenuItem("Mapping/Mapping Tool")]
		public static void ShowWindow()
		{
			var window = GetWindow<MappingTool>(true);
			window.minSize = new Vector2(500, 500);
			window.Show();
		}

		public void OnEnable()
		{
			SceneView.duringSceneGui += DrawSceneGUI;
		}
		
		public void OnDisable()
		{
			SceneView.duringSceneGui -= DrawSceneGUI;
		}
		
		public void OnGUI()
		{
			ShowCenterOfMass = EditorGUILayout.ToggleLeft("Show Center of Mass", ShowCenterOfMass);
			CenterOfMassSize = EditorGUILayout.Slider("Center of Mass Size", CenterOfMassSize, 0.01f, 1f);
			
			foreach (var pair in transforms)
			{
				GUILayout.BeginHorizontal();
				
				GUILayout.Label($"{pair.Key} ({pair.Value.Count})");
				
				if (GUILayout.Button("Apply Transforms", GUILayout.Width(125)))
				{
					var components = FindObjectsByType<Component>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
					for (var i = 0; i < components.Length; i++)
					{
						var component = components[i];
						if (component is not IObject iObject || string.IsNullOrWhiteSpace(iObject.ObjectID) || iObject.ExternallySpawned || iObject is NPCSpawner or BaseLight or BaseDoor or BaseElevator or BaseConveyor)
							continue;
						
						var go = component.gameObject;
						if (go.isStatic)
							continue;
						
						var type = iObject.GetType();

						if (!transforms.TryGetValue(type, out var dict))
							continue;
						
						if (!dict.TryGetValue(iObject.ObjectID, out var tuple))
							continue;

						var tr = component.transform;
						tr.position = tuple.Item1;
						tr.eulerAngles = tuple.Item2;
						tr.localScale = tuple.Item3;
						
						EditorUtility.SetDirty(go);
					}
				}
				
				GUILayout.EndHorizontal();
			}
			
			GUILayout.FlexibleSpace();

			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Grab Transforms"))
			{
				transforms.Clear();

				var components = FindObjectsByType<Component>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
				for (var i = 0; i < components.Length; i++)
				{
					var component = components[i];
					if (component is not IObject iObject || string.IsNullOrWhiteSpace(iObject.ObjectID) || iObject.ExternallySpawned || iObject is NPCSpawner or BaseLight or BaseDoor or BaseElevator or BaseConveyor)
						continue;

					var go = component.gameObject;
					if (go.isStatic)
						continue;
					
					var type = iObject.GetType();

					if (!transforms.TryGetValue(type, out var dict))
					{
						dict = new Dictionary<string, Tuple<Vector3, Vector3, Vector3>>();
						transforms[type] = dict;
					}

					var tr = component.transform;
					dict[iObject.ObjectID] = new Tuple<Vector3, Vector3, Vector3>(tr.position, tr.eulerAngles, tr.localScale);
				}
			}

			if (GUILayout.Button("Clear Transforms"))
			{
				transforms.Clear();
			}
			
			GUILayout.EndHorizontal();
			
			SceneView.RepaintAll();
		}

		public void DrawSceneGUI(SceneView sceneView)
		{
			var previousColor = Handles.color;
			Handles.color = Color.green;

			var selection = Selection.gameObjects;
			for (var i = 0; i < selection.Length; i++)
			{
				var go = selection[i];
				
				var rigidBodies = go.GetComponentsInChildren<Rigidbody>(false);
				for (var k = 0; k < rigidBodies.Length; k++)
				{
					var rigidBody = rigidBodies[k];
					
					Handles.color = Color.red;
					Handles.SphereHandleCap(1, rigidBody.transform.TransformPoint(rigidBody.centerOfMass), rigidBody.rotation, CenterOfMassSize, EventType.Repaint);
				}
			}
			
			Handles.color = previousColor;
		}
	}
}