using System;
using Components;
using Objects.Base;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(DelayedTrigger), true), CanEditMultipleObjects]
	public class DelayedTriggerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var delayedTriggers = targets;
				for (var i = 0; i < delayedTriggers.Length; i++)
				{
					var delayedTrigger = (DelayedTrigger)delayedTriggers[i];
					delayedTrigger.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(delayedTrigger);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}