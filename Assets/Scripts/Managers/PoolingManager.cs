using System;
using System.Collections.Generic;
using ScriptableObjects;
using UnityEngine;

namespace Managers
{
	public class PoolingManager : MonoBehaviour
	{
		public static PoolingManager Instance;

		[SerializeField]
		public Dictionary<Type, List<GameObject>> Pool = new ();

		public Dictionary<Data, List<GameObject>> DataPool = new ();
		
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
		
		public void AddToPool(Data data, GameObject go, bool disable = true)
		{
			if (DataPool.TryGetValue(data, out var list))
			{
				if (IsPooled(data, go))
					return;
			}
			else
			{
				list = new List<GameObject>();
				DataPool[data] = list;
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
		
		public GameObject TakeFromPool(Data data, bool enable)
		{
			if (!DataPool.TryGetValue(data, out var list) || list.Count == 0)
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
		
		public void ClearPool(Data data)
		{
			DataPool.Remove(data);
		}

		public void ClearPool()
		{
			Pool.Clear();
			DataPool.Clear();
		}
		
		public bool IsPooled(Type type, GameObject go)
		{
			return Pool.TryGetValue(type, out var list) && list.Contains(go);
		}
		
		public bool IsPooled(Data data, GameObject go)
		{
			return DataPool.TryGetValue(data, out var list) && list.Contains(go);
		}
	}
}