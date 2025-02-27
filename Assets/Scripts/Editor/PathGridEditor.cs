using AI.PathFinding;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(PathGrid), true)]
	public class PathGridEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Grid"))
			{
				var pathGrid = (PathGrid)target;
				pathGrid.CreateGrid();
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(pathGrid.gameObject.scene);
				SceneView.RepaintAll();
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}