using System;
using Objects.Base;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(BaseObject), true), CanEditMultipleObjects]
	public class BaseObjectEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate Object ID"))
			{
				var baseObjects = targets;
				for (var i = 0; i < baseObjects.Length; i++)
				{
					var baseObject = (BaseObject)baseObjects[i];
					baseObject.ObjectID = Guid.NewGuid().ToString();
					
					EditorUtility.SetDirty(baseObject);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}