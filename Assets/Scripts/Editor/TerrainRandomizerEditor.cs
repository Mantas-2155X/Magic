using Tools;
using UnityEditor;
using UnityEngine;
using World;

namespace Editor
{
	[CustomEditor(typeof(TerrainGenerator))]
	public class TerrainGeneratorEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			var generator = (TerrainGenerator)target;
			
			GUILayout.BeginHorizontal();
			
			if (GUILayout.Button("Generate Terrain"))
			{
				generator.Generate();
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
			}
			
			if (GUILayout.Button("Paint Terrain"))
			{
				generator.Paint();
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
			}
			
			GUILayout.EndHorizontal();

			GUILayout.Space(10);
			
			GUILayout.BeginHorizontal();
			
			var seed = generator.Seed;
			
			if (seed == null || seed.Length != 2)
				seed = new[] { 0f, 0f };
			
			seed[0] = EditorGUILayout.FloatField(seed[0]);
			seed[1] = EditorGUILayout.FloatField(seed[1]);
			generator.Seed = seed;
			
			if (GUILayout.Button("Random", GUILayout.Width(75)))
			{
				generator.RandomizeSeed();
				generator.Generate();
				generator.Paint();
				UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(generator.gameObject.scene);
			}
			
			GUILayout.EndHorizontal();
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}