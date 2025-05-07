using Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(StateManager), true)]
	public class StateManagerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Save State"))
				((StateManager)target).Save();
			
			if (GUILayout.Button("Load State"))
				((StateManager)target).Load();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}