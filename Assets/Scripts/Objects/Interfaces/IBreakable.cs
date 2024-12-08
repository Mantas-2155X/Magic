using UnityEngine;

namespace Objects.Interfaces
{
	public interface IBreakable
	{
		public int Health { get; }
		
		public bool IsBroken { get; }

		public void Damage(int damage, object source);
		public void Break(object source);

		public GameObject GetGameObject();
	}
}