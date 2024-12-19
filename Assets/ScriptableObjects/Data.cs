using UnityEngine;

namespace ScriptableObjects
{
	public class Data : ScriptableObject
	{
		[SerializeField]
		public string Name;
		
		[SerializeField]
		public string Description;

		[SerializeField]
		public GameObject Prefab;
	}
}