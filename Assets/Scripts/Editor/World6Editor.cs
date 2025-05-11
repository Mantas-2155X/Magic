using System;
using Scenes;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(World6), true), CanEditMultipleObjects]
	public class World6Editor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var world6s = targets;
				for (var i = 0; i < world6s.Length; i++)
				{
					var world6 = (World6)world6s[i];
					world6.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(world6);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}