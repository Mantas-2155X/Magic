using Tools;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(ThumbnailGenerator), true)]
	public class ThumbnailGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Take"))
			{
				var generator = (ThumbnailGenerator)target;
				generator.Take();
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}