using AI.Interfaces;
using Objects.Enums;
using UnityEngine;

namespace Objects.Interfaces
{
	public interface IUsable
	{
		public float UsableAfter { get; }
		public EDestroyType DestroyAfterUse { get; }
		
		public bool CanUse(IAlive user);
		public bool Use(IAlive user);
		
		public GameObject GetGameObject();
	}
}