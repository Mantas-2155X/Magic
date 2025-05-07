using System;
using Objects;
using UnityEditor;
using UnityEngine;

namespace Editor
{
	[CustomEditor(typeof(NPCSpawner), true), CanEditMultipleObjects]
	public class NPCSpawnerEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			GUILayout.Space(5);
			
			if (GUILayout.Button("Generate SpawnIDs"))
			{
				var npcSpawners = targets;
				for (var i = 0; i < npcSpawners.Length; i++)
				{
					var npcSpawner = (NPCSpawner)npcSpawners[i];

					if (npcSpawner.SpawnCount != npcSpawner.SpawnIDs.Length)
					{
						var ids = npcSpawner.SpawnIDs;
						Array.Resize(ref ids, npcSpawner.SpawnCount);
						npcSpawner.SpawnIDs = ids;
					}

					for (var k = 0; k < npcSpawner.SpawnCount; k++)
					{
						var spawnID = npcSpawner.SpawnIDs[k];
						
						// Already assigned a spawn ID, keep it to remain save compat
						if (!string.IsNullOrEmpty(spawnID))
							continue;
						
						npcSpawner.SpawnIDs[k] = Guid.NewGuid().ToString();
					}
					
					EditorUtility.SetDirty(npcSpawner);
				}
			}
			
			serializedObject.ApplyModifiedProperties();
		}
	}
}