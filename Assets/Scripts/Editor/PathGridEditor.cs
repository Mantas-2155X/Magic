using System;
using System.Collections.Generic;
using AI.PathFinding;
using AI.PathFinding.Structs;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Editor
{
	[CustomEditor(typeof(PathGrid))]
	public class PathGridEditor : UnityEditor.Editor
	{
		private List<SNode[]> foundPaths = new ();
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			var pathGrid = (PathGrid)target;
			
			if (GUILayout.Button("Create Grid"))
				pathGrid.CreateGrid().Forget();
			
			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Find 1 Path"))
				findPath(pathGrid).Forget();
			
			if (GUILayout.Button("Find 5 Paths"))
				for (var i = 0; i < 5; i++)
					findPath(pathGrid).Forget();
			
			GUILayout.EndHorizontal();
			
			if (GUILayout.Button("Clear Paths"))
				foundPaths.Clear();
			
			serializedObject.ApplyModifiedProperties();
			
			SceneView.RepaintAll();
		}
		
		private async UniTask findPath(PathGrid pathGrid)
		{
			var startPos = new Vector3(Random.Range(-pathGrid.Size.x, pathGrid.Size.x), Random.Range(-pathGrid.Size.y, pathGrid.Size.y), Random.Range(-pathGrid.Size.z, pathGrid.Size.z));
			var endPos = new Vector3(Random.Range(-pathGrid.Size.x, pathGrid.Size.x), Random.Range(-pathGrid.Size.y, pathGrid.Size.y), Random.Range(-pathGrid.Size.z, pathGrid.Size.z));
		
			var path = await pathGrid.FindPath(startPos, endPos);
			foundPaths.Add(path);
		}

		public void OnSceneGUI()
		{
			if (foundPaths == null || foundPaths.Count <= 0)
				return;

			var currentColor = Handles.color;
			Handles.color = Color.cyan;
				
			foreach (var foundPath in foundPaths)
			{
				if (foundPath == null)
					continue;
				
				for (var i = 0; i < foundPath.Length - 1; i++)
				{
					var nodePos = foundPath[i].WorldPosition;
					var otherNodePos = foundPath[i + 1].WorldPosition;
					
					Handles.DrawLine(nodePos, otherNodePos);
				}
			}
			
			Handles.color = currentColor;
		}
	}
}