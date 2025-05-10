using Managers;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(World.World), true), CanEditMultipleObjects]
	public class WorldEditor : UnityEditor.Editor
	{
		[SerializeField]
		public bool[] Folds = new bool[4];
		
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);

			Folds[0] = EditorGUILayout.Foldout(Folds[0], $"Destroyed Objects ({StateManager.Instance.DestroyedObjects.Count})", true);
			if (Folds[0])
			{
				var list = StateManager.Instance.DestroyedObjects;
				for (var i = 0; i < list.Count; i++)
					EditorGUILayout.TextField(list[i]);
			}
			
			Folds[1] = EditorGUILayout.Foldout(Folds[1], $"Destroyed Components ({StateManager.Instance.DestroyedComponents.Count})", true);
			if (Folds[1])
			{
				var list = StateManager.Instance.DestroyedComponents;
				for (var i = 0; i < list.Count; i++)
					EditorGUILayout.TextField(list[i]);
			}
			
			Folds[2] = EditorGUILayout.Foldout(Folds[2], $"Killed Alives ({StateManager.Instance.KilledAlives.Count})", true);
			if (Folds[2])
			{
				var list = StateManager.Instance.KilledAlives;
				for (var i = 0; i < list.Count; i++)
					EditorGUILayout.TextField(list[i]);
			}
			
			Folds[3] = EditorGUILayout.Foldout(Folds[3], $"Registered Objects ({StateManager.Instance.GetRegisteredObjects().Count})", true);
			if (Folds[3])
			{
				foreach (var pair in StateManager.Instance.GetRegisteredObjects())
				{
					GUILayout.BeginHorizontal();
					EditorGUILayout.TextField(pair.Key, GUILayout.Width(175));
					EditorGUILayout.ObjectField((Object)pair.Value, typeof(Object), true);
					GUILayout.EndHorizontal();
				}
			}
			
			GUILayout.Space(5);
			
			if (GUILayout.Button("Save State"))
				StateManager.Instance.Save();
			
			var saves = StateManager.Instance.AvailableSaves;
			foreach (var pair in saves)
			{
				GUILayout.BeginHorizontal();
				
				if (GUILayout.Button($"Load {pair.Key}"))
					StateManager.Instance.Load(pair.Value);
				
				if (GUILayout.Button("X", GUILayout.Width(25)))
				{
					StateManager.Instance.Delete(pair.Key);
					break;
				}
				
				GUILayout.EndHorizontal();
			}
			
			serializedObject.ApplyModifiedProperties();
			Repaint();
		}
	}
}