using Combat.Weapons.Base;
using Objects;
using UnityEditor;
using UnityEngine;

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
				
				weapon.Rigidbody = go.AddComponent<Rigidbody>();
				weapon.Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
				weapon.Rigidbody.mass = 5f;

				weapon.DroppedWeapon = go.AddComponent<DroppedWeapon>();
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}