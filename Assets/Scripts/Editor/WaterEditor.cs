using System;
using Scenes;
using UnityEditor;
using UnityEngine;
using World;

namespace Editor
{
	[CustomEditor(typeof(Water), true), CanEditMultipleObjects]
	public class WaterEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var waters = targets;
				for (var i = 0; i < waters.Length; i++)
				{
					var water = (Water)waters[i];
					water.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(water);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}