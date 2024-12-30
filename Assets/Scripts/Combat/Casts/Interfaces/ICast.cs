using ScriptableObjects;
using UnityEngine;

namespace Combat.Casts.Interfaces
{
	public interface ICast
	{
		public CastData CastData { get; }

		public Component Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(Component source);

		public void StopParticles();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}