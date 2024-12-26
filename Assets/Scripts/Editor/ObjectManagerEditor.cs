using Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(ObjectManager), true)]
	public class ObjectManagerEditor : UnityEditor.Editor
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
		
			var objects = ((ObjectManager)target).GetRegisteredObjects();

			foreach (var obj in objects)
				EditorGUILayout.ObjectField((Component)obj, typeof(Component), true);
			
			GUILayout.Space(5);
			
			base.OnInspectorGUI();
			serializedObject.ApplyModifiedProperties();
			
			Repaint();
		}
	}
}