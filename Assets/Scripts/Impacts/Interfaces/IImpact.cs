using Projectiles.Interfaces;
using UnityEngine;

namespace Impacts.Interfaces
{
	public interface IImpact
	{
		public IProjectile Source { get; }
		
		public void Spawn(IProjectile source, Vector3 position, Quaternion angles, bool parent);
		
		public GameObject GetGameObject();
	}
}