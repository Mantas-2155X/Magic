using System;
using Components;
using Objects.Base;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(Trigger), true), CanEditMultipleObjects]
	public class TriggerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var triggers = targets;
				for (var i = 0; i < triggers.Length; i++)
				{
					var trigger = (Trigger)triggers[i];
					trigger.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(trigger);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}