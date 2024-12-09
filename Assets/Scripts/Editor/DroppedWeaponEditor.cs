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

			if (GUILayout.Button("Grab Colliders"))
			{
				var weapon = (BaseWeapon)target;
				weapon.Colliders = weapon.transform.GetComponentsInChildren<Collider>();
				
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(weapon.gameObject.scene);
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}