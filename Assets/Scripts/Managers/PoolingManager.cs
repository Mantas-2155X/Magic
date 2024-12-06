using System;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
	public class PoolingManager : MonoBehaviour
	{
		public static PoolingManager Instance;

		[SerializeField]
		public Dictionary<Type, List<GameObject>> Pool = new ();
		
		public void Awake()
		{
			Instance = this;
		}

		public void AddToPool(Type type, GameObject go, bool disable = true)
		{
			if (Pool.TryGetValue(type, out var list))
			{
				if (IsPooled(type, go))
					return;
			}
			else
			{
				list = new List<GameObject>();
				Pool[type] = list;
			}
			
			if (disable)
				go.SetActive(false);
			
			list.Add(go);
		}

		public GameObject TakeFromPool(Type type, bool enable)
		{
			if (!Pool.TryGetValue(type, out var list) || list.Count == 0)
				return null;

			var go = list[0];
			list.Remove(go);
			
			if (enable)
				go.SetActive(true);
			
			return go;
		}
		
		public void ClearPool(Type type)
		{
			Pool.Remove(type);
		}

		public void ClearPool()
		{
			Pool.Clear();
		}
		
		public bool IsPooled(Type type, GameObject go)
		{
			return Pool.TryGetValue(type, out var list) && list.Contains(go);
		}
	}
}