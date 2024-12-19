using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Managers
{
	public class PoolingManager : MonoBehaviour
	{
		public static PoolingManager Instance;

		public readonly Dictionary<Data, List<GameObject>> Pool = new ();
		
		public void Awake()
		{
			Instance = this;
		}

		public void AddToPool(Data data, GameObject go, bool disable = true)
		{
			if (Pool.TryGetValue(data, out var list))
			{
				if (IsPooled(data, go))
					return;
			}
			else
			{
				list = new List<GameObject>();
				Pool[data] = list;
			}
			
			if (disable)
				go.SetActive(false);
			
			list.Add(go);
		}

		public GameObject TakeFromPool(Data data, bool enable)
		{
			if (!Pool.TryGetValue(data, out var list) || list.Count == 0)
				return null;

			var go = list[0];
			list.Remove(go);
			
			if (enable)
				go.SetActive(true);
			
			return go;
		}
		
		public void ClearPool(Data data)
		{
			Pool.Remove(data);
		}

		public void ClearPool()
		{
			Pool.Clear();
		}
		
		public bool IsPooled(Data data, GameObject go)
		{
			return Pool.TryGetValue(data, out var list) && list.Contains(go);
		}
	}
}