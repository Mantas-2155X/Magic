using System.Collections.Generic;
using AI.PathFinding;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Grid = AI.PathFinding.Grid;
using Random = UnityEngine.Random;

namespace Editor
{
	[CustomEditor(typeof(Grid))]
	public class GridEditor : UnityEditor.Editor
	{
		private List<Path> foundPaths = new ();
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			var grid = (Grid)target;
			
			if (GUILayout.Button("Create Grid"))
				grid.CreateGrid().Forget();
			
			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Find 1 Path"))
				findPath(grid).Forget();
			
			if (GUILayout.Button("Find 5 Paths"))
				for (var i = 0; i < 5; i++)
					findPath(grid).Forget();
			
			GUILayout.EndHorizontal();
			
			if (GUILayout.Button("Clear Paths"))
				foundPaths.Clear();
			
			serializedObject.ApplyModifiedProperties();
			
			SceneView.RepaintAll();
		}
		
		private async UniTask findPath(Grid grid)
		{
			var startPos = new Vector3(Random.Range(-grid.Size.x, grid.Size.x), Random.Range(-grid.Size.y, grid.Size.y), Random.Range(-grid.Size.z, grid.Size.z));
			var endPos = new Vector3(Random.Range(-grid.Size.x, grid.Size.x), Random.Range(-grid.Size.y, grid.Size.y), Random.Range(-grid.Size.z, grid.Size.z));
		
			var path = await grid.FindPath(startPos, endPos);
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
				
				var points = foundPath.Points;
				for (var i = 0; i < points.Length - 1; i++)
				{
					var nodePos = points[i];
					var otherNodePos = points[i + 1];
					
					Handles.DrawLine(nodePos, otherNodePos);
				}
			}
			
			Handles.color = currentColor;
		}
	}
}