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

		public void Add(Data data, GameObject go, bool disable = true)
		{
			if (Pool.TryGetValue(data, out var list))
			{
				if (list.Contains(go))
					return;
			}
			else
			{
				list = new List<GameObject>();
				Pool[data] = list;
			}
			
			list.Add(go);
			
			if (disable)
				go.SetActive(false);
		}

		public GameObject Take(Data data, bool enable)
		{
			if (!Pool.TryGetValue(data, out var list) || list.Count == 0)
				return null;

			var go = list[0];
			list.Remove(go);
			
			if (enable)
				go.SetActive(true);
			
			return go;
		}
		
		public T TakeOrCreate<T>(Data data, bool enablePooled)
		{
			var obj = Take(data, enablePooled);
			
			if (obj == null)
				obj = Instantiate(data.Prefab);

			return obj.GetComponent<T>();
		}
		
		public void Clear(Data data)
		{
			Pool.Remove(data);
		}

		public void Clear()
		{
			Pool.Clear();
		}
	}
}