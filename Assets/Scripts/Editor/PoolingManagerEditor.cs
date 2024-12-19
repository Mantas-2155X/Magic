using Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(PoolingManager), true)]
	public class PoolingManagerEditor : UnityEditor.Editor
	{
		[SerializeField]
		public bool ShowStats;

		public override void OnInspectorGUI()
		{
			var pool = (PoolingManager)target;
			if (GUILayout.Button("Clear"))
				pool.Clear();

			ShowStats = EditorGUILayout.ToggleLeft("Show Stats", ShowStats);
			if (!ShowStats)
			{
				base.OnInspectorGUI();
				serializedObject.ApplyModifiedProperties();
				return;
			}
			
			foreach (var pair in pool.Pool)
			{
				EditorGUILayout.LabelField(pair.Key.Name, GUILayout.Width(150));
				
				foreach (var go in pair.Value)
					EditorGUILayout.ObjectField(go, typeof(GameObject), true);
				
				GUILayout.Space(5);
			}
			
			GUILayout.Space(5);
			
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
		}
	}
}