using AI.Interfaces;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IUsable
	{
		public float UsableAfter { get; }
		public bool DestroyAfterUse { get; }
		
		public bool CanUse(IAlive user);
		public bool Use(IAlive user);
		
		public GameObject GetGameObject();
	}
}