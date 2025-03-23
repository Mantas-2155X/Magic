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

		[SerializeField]
		private bool createGridLoop;

		[SerializeField]
		private bool findPathLoop;
		
		[SerializeField]
		private Vector3 customStartPos;
		[SerializeField]
		private Vector3 customEndPos;
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			
			GUILayout.Space(30);

			var grid = (Grid)target;

			GUILayout.BeginHorizontal();
			EditorGUIUtility.labelWidth = 50;
			EditorGUILayout.IntField("WAIT", grid.WaitingPathFinds);
			EditorGUILayout.IntField("DELAY", grid.DelayedPathFinds);
			EditorGUILayout.IntField("ACT", grid.ActivePathFinds);
			EditorGUIUtility.labelWidth = 0;
			GUILayout.EndHorizontal();
			
			if (GUILayout.Button("Create Grid"))
				createGrid(grid).Forget();
			
			createGridLoop = EditorGUILayout.Toggle("Loop Grid", createGridLoop);
			
			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Find 1 Path"))
				findPath(grid).Forget();
			
			if (GUILayout.Button("Find 5 Paths"))
				for (var i = 0; i < 5; i++)
					findPath(grid).Forget();
			
			GUILayout.EndHorizontal();

			customStartPos = EditorGUILayout.Vector3Field("Custom Start Position", customStartPos);
			customEndPos = EditorGUILayout.Vector3Field("Custom End Position", customEndPos);
			
			findPathLoop = EditorGUILayout.Toggle("Loop Paths", findPathLoop);

			if (GUILayout.Button("Find Custom Path"))
				findPath(grid, customStartPos, customEndPos).Forget();
			
			if (GUILayout.Button("Clear Paths"))
				foundPaths.Clear();
			
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
			SceneView.RepaintAll();
		}

		private async UniTask createGrid(Grid grid)
		{
			await grid.CreateGrid();

			if (createGridLoop)
			{
				await UniTask.WaitForSeconds(0.5f);
				createGrid(grid).Forget();
			}
		}
		
		private async UniTask findPath(Grid grid, Vector3? startPos = null, Vector3? endPos = null)
		{
			if (startPos == null)
				startPos = new Vector3(Random.Range(-grid.Size.x, grid.Size.x), Random.Range(-grid.Size.y, grid.Size.y), Random.Range(-grid.Size.z, grid.Size.z));
		
			if (endPos == null)
				endPos = new Vector3(Random.Range(-grid.Size.x, grid.Size.x), Random.Range(-grid.Size.y, grid.Size.y), Random.Range(-grid.Size.z, grid.Size.z));
		
			var path = await grid.FindPath(startPos.Value, endPos.Value);
			foundPaths.Add(path);

			if (findPathLoop)
			{
				findPath(grid, startPos, endPos).Forget();
				
				await UniTask.NextFrame();

				foundPaths.Remove(path);
			}
		}

		public void OnSceneGUI()
		{
			if (foundPaths == null || foundPaths.Count <= 0)
				return;

			var grid = (Grid)target;

			var currentColor = Handles.color;
				
			foreach (var foundPath in foundPaths)
			{
				if (foundPath == null)
					continue;
				
				if (grid.DrawPaths)
				{
					Handles.color = Color.cyan;
					
					var points = foundPath.Points;
					for (var i = 0; i < points.Count - 1; i++)
					{
						var nodePos = points[i];
						var otherNodePos = points[i + 1];
					
						Handles.DrawLine(nodePos, otherNodePos);
					}
				}

				if (grid.DrawNodes)
				{
					if (grid.DrawSearched)
					{
						Handles.color = Color.magenta;
					
						var searched = foundPath.Searched;
						for (var i = 0; i < searched.Count; i++)
							Handles.SphereHandleCap(0, searched[i], Quaternion.identity, foundPath.NodeRadius, EventType.Repaint);
					}
				}
			}
			
			Handles.color = currentColor;
		}
	}
}