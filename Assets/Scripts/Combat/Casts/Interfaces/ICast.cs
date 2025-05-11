using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Casts.Interfaces
{
	public interface ICast : IIdentifiable
	{
		public CastData CastData { get; }

		public IIdentifiable Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(IIdentifiable source);

		public IAlive GetAlive();

		public void StopParticles();
	}
}