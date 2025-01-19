using ScriptableObjects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(Path), true)]
	public class PathEditor : UnityEditor.Editor
	{
		[SerializeField]
		public int SelectedPoint;
		
		public void OnEnable()
		{
			SceneView.duringSceneGui += DrawSceneGUI;
		}
		
		public void OnDisable()
		{
			SceneView.duringSceneGui -= DrawSceneGUI;
		}

		public override void OnInspectorGUI()
		{
			var path = (Path)target;
			
			GUILayout.Label($"Points ({path.Points.Count})", EditorStyles.boldLabel);
			
			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Add"))
			{
				var point = Vector3.zero;
				var pause = 0f;

				if (path.Points.Count > SelectedPoint)
				{
					var pathPoint = path.Points[SelectedPoint];
					point = pathPoint.Point;
					pause = pathPoint.Pause;
				}
				
				path.Points.Add(new Path.SPathPoint { Point = point, Pause = pause });
				SelectedPoint = path.Points.Count - 1;
			}

			if (GUILayout.Button("Clear"))
				path.Points.Clear();

			GUILayout.EndHorizontal();
			
			GUILayout.BeginVertical();

			for (var i = 0; i < path.Points.Count; i++)
			{
				var pathPoint = path.Points[i];
				
				GUILayout.BeginHorizontal();

				EditorGUIUtility.labelWidth = 30;
				pathPoint.Point = EditorGUILayout.Vector3Field($"{i}{(i == SelectedPoint ? "*" : "")}", pathPoint.Point);
				EditorGUIUtility.labelWidth = 12;
				pathPoint.Pause = EditorGUILayout.FloatField("P", pathPoint.Pause, GUILayout.Width(55));
				EditorGUIUtility.labelWidth = 0;

				if (GUILayout.Button("Select", GUILayout.Width(55)))
					SelectedPoint = i;

				if (GUILayout.Button("/\\", GUILayout.Width(25)))
				{
					if (i == 0)
						return;

					if (SelectedPoint == i)
						SelectedPoint = i - 1;
					else if (SelectedPoint == i - 1)
						SelectedPoint = i;
					
					var currentPoint = path.Points[i];
					var targetPoint = path.Points[i - 1];

					path.Points[i - 1] = currentPoint;
					path.Points[i] = targetPoint;
					return;
				}
				
				if (GUILayout.Button("\\/", GUILayout.Width(25)))
				{
					if (i == path.Points.Count - 1)
						return;
					
					if (SelectedPoint == i)
						SelectedPoint = i + 1;
					else if (SelectedPoint == i + 1)
						SelectedPoint = i;

					var currentPoint = path.Points[i];
					var targetPoint = path.Points[i + 1];

					path.Points[i + 1] = currentPoint;
					path.Points[i] = targetPoint;
					return;
				}
				
				if (GUILayout.Button("-", GUILayout.Width(20)))
				{
					path.Points.RemoveAt(i);
					return;
				}

				GUILayout.EndHorizontal();
				
				path.Points[i] = pathPoint;
			}
			
			GUILayout.EndVertical();

			EditorUtility.SetDirty(path);
			serializedObject.ApplyModifiedProperties();
			
			SceneView.RepaintAll();
		}

		public void DrawSceneGUI(SceneView sceneView)
		{
			var path = (Path)target;

			var previousColor = Handles.color;
			Handles.color = Color.green;
			
			for (var i = 0; i < path.Points.Count; i++)
			{
				if (i == path.Points.Count - 1)
					continue;
				
				Handles.DrawLine(path.Points[i].Point, path.Points[i + 1].Point);
			}
			
			Handles.color = previousColor;
			
			if (path.Points.Count <= SelectedPoint)
				return;

			var pathPoint = path.Points[SelectedPoint];
			pathPoint.Point = Handles.PositionHandle(pathPoint.Point, Quaternion.identity);
			
			path.Points[SelectedPoint] = pathPoint;
		}
	}
}