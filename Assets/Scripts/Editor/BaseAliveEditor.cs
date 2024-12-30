using AI.Base;
using AI.Interfaces;
using ScriptableObjects;
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
				EditorGUILayout.ObjectField(obj.SpellData, typeof(SpellData), true);
			
			GUILayout.Space(5);
			
			GUILayout.Label("Wearables");

			foreach (var obj in alive.Wearables)
				EditorGUILayout.ObjectField(obj.WearableData, typeof(WearableData), true);
			
			GUILayout.Space(5);
			
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
		}
	}
}