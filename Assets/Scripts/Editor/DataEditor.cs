/*using ScriptableObjects;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Editor
{
	public static class DataEditor
	{
		[MenuItem("Data/Convert Datas To Addressables")]
		public static void ConvertToAddressables()
		{
			var assetPaths = AssetDatabase.GetAllAssetPaths();
			for (var i = 0; i < assetPaths.Length; i++)
			{
				var assetPath = assetPaths[i];
				
				var data = AssetDatabase.LoadAssetAtPath<Data>(assetPath);
				if (data == null)
					continue;
				
				if (data is AliveData aliveData)
				{
					aliveData.PrefabReference = convertReference(aliveData.Prefab);
					aliveData.BrokenBodyPrefabReference = convertReference(aliveData.BrokenBodyPrefab);
					aliveData.BrokenArmPrefabReference = convertReference(aliveData.BrokenArmPrefab);
					aliveData.BrokenFootPrefabReference = convertReference(aliveData.BrokenFootPrefab);
				}
				else if (data is ObjectData objectData)
				{
					objectData.PrefabReference = convertReference(objectData.Prefab);
					objectData.BrokenPrefabReference = convertReference(objectData.BrokenPrefab);
				}
				else
				{
					data.PrefabReference = convertReference(data.Prefab);
				}
				
				EditorUtility.SetDirty(data);
			}
		}

		private static AssetReference convertReference(GameObject prefab)
		{
			if (prefab == null)
				return null;
			
			var prefabPath = AssetDatabase.GetAssetPath(prefab);
			var prefabGUID = AssetDatabase.AssetPathToGUID(prefabPath);

			return new AssetReference(prefabGUID);
		}
	}
}*/