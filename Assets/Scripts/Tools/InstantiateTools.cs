using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tools
{
	public static class InstantiateTools
	{
		public static GameObject Instantiate(AssetReference assetReference, GameObject prefab, Transform parent)
		{
			if (assetReference != null && !string.IsNullOrWhiteSpace(assetReference.AssetGUID))
				return Addressables.InstantiateAsync(assetReference, parent).WaitForCompletion();
			
			if (prefab != null)
				return Object.Instantiate(prefab, parent);

			return null;
		}
	}
}