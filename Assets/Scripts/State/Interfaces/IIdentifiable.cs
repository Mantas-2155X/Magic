using UnityEngine;

namespace State.Interfaces
{
	public interface IIdentifiable
	{
		public string ObjectID { get; set; }
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}