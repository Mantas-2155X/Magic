using Combat.Wearables.Base;
using Objects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(BaseWearable), true)]
	public class BaseWearableEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Setup"))
			{
				var wearable = (BaseWearable)target;
				
				var go = wearable.gameObject;
				
				DestroyImmediate(go.GetComponent<DroppedWearable>());
				DestroyImmediate(go.GetComponent<Rigidbody>());
				
				wearable.Colliders = go.GetComponentsInChildren<Collider>(true);
				
				for (var i = 0; i < wearable.Colliders.Length; i++)
					wearable.Colliders[i].enabled = true;
				
				wearable.Rigidbody = go.AddComponent<Rigidbody>();
				wearable.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				wearable.Rigidbody.mass = 5f;

				wearable.DroppedWearable = go.AddComponent<DroppedWearable>();
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(wearable.gameObject.scene);
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}