using AI.Base;
using AI.Interfaces;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(BaseAlive), true)]
	public class BaseAliveEditor : UnityEditor.Editor
	{
		[SerializeField]
		public bool ShowStats;

		public override void OnInspectorGUI()
		{
			ShowStats = EditorGUILayout.ToggleLeft("Show Stats", ShowStats);
			if (!ShowStats)
			{
				base.OnInspectorGUI();
				serializedObject.ApplyModifiedProperties();
				return;
			}

			var alive = (IAlive)target;
			
			GUILayout.Label("Spells");
			
			foreach (var obj in alive.Spells)
				EditorGUILayout.ObjectField((Component)obj, typeof(Component), true);
			
			GUILayout.Space(5);
			
			GUILayout.Label("Wearables");

			foreach (var obj in alive.Wearables)
				EditorGUILayout.ObjectField((Component)obj, typeof(Component), true);
			
			GUILayout.Space(5);
			
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
		}
	}
}