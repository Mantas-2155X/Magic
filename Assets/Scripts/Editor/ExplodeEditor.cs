using Components;
using UnityEditor;
using UnityEngine;
using World;

namespace Editor
{
	[CustomEditor(typeof(Explode), true)]
	public class ExplodeEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Grab Rigidbodies"))
			{
				var explode = (Explode)target;
				explode.Rigidbodies = explode.GetComponentsInChildren<Rigidbody>(true);
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(explode.gameObject.scene);
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}