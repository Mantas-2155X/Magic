using UnityEngine;

namespace Casts.Interfaces
{
	public interface ICast
	{
		public Component Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(Component source);

		public void StopParticles();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}