using Objects;
using UnityEditor;
using UnityEngine;
using Weapons.Base;

namespace Editor
{
	[CustomEditor(typeof(BaseWeapon), true)]
	public class BaseWeaponEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Setup"))
			{
				var weapon = (BaseWeapon)target;
				
				var go = weapon.gameObject;
				
				DestroyImmediate(go.GetComponent<DroppedWeapon>());
				DestroyImmediate(go.GetComponent<Rigidbody>());
				
				weapon.Colliders = go.GetComponentsInChildren<Collider>(true);
				
				for (var i = 0; i < weapon.Colliders.Length; i++)
					weapon.Colliders[i].enabled = true;
				
				var rb = go.AddComponent<Rigidbody>();
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.mass = 5f;

				go.AddComponent<DroppedWeapon>();
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}