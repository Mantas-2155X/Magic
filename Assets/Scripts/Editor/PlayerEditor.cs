using System;
using AI;
using AI.Base;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace Editor
{
	[CustomEditor(typeof(Player), true)]
	public class PlayerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			if (GUILayout.Button("Show"))
				((Player)target).SetRenderMode(ShadowCastingMode.On);
			
			if (GUILayout.Button("Hide"))
				((Player)target).SetRenderMode(ShadowCastingMode.ShadowsOnly);
			
			generateID();

			serializedObject.ApplyModifiedProperties();
		}
		
		private void generateID()
		{
			if (!GUILayout.Button("Generate Object ID"))
				return;

			var baseObject = (Player)target;
			baseObject.ObjectID = Guid.NewGuid().ToString();
					
			EditorUtility.SetDirty(baseObject);
		}
	}
}