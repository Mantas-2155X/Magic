using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Casts.Interfaces
{
	public interface ICast : IIdentifiable
	{
		public CastData CastData { get; }

		public Component Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(Component source);

		public IAlive GetAlive();

		public void StopParticles();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}