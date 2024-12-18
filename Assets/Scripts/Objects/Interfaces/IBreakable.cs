using UnityEngine;

namespace Objects.Interfaces
{
	public interface IBreakable
	{
		public float Health { get; }
		
		public bool IsBroken { get; }

		public void Damage(float damage, object source);
		public void Break(object source);

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}