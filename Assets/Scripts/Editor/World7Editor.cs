using System;
using Scenes;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(World7), true), CanEditMultipleObjects]
	public class World7Editor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var world7s = targets;
				for (var i = 0; i < world7s.Length; i++)
				{
					var world7 = (World7)world7s[i];
					world7.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(world7);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}