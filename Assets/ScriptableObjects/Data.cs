using ScriptableObjects.Enums;
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

		[SerializeField]
		public Sprite Icon;
		
		[SerializeField]
		public ETag Tags;
		
		[Header("Instantiation")]
		[SerializeField]
		public GameObject Prefab;
		
		[SerializeField]
		public string Type;
		
		[SerializeField]
		public string Assembly;
	}
}