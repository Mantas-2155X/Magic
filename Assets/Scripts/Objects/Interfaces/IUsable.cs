using AI.Interfaces;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IUsable
	{
		public bool DestroyAfterUse { get; }
		
		public void Use(IAlive user);
		
		public GameObject GetGameObject();
	}
}