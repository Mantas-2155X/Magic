using System;
using Scenes;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(World4), true), CanEditMultipleObjects]
	public class World4Editor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var world4s = targets;
				for (var i = 0; i < world4s.Length; i++)
				{
					var world4 = (World4)world4s[i];
					world4.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(world4);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}