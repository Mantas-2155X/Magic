using UnityEngine;

namespace ScriptableObjects
{
	public class Data : ScriptableObject
	{
		[Header("Basic")]
		[SerializeField]
		public string Name;
		
		[SerializeField]
		public string Description;

		[Header("Instantiation")]
		[SerializeField]
		public GameObject Prefab;
	}
}