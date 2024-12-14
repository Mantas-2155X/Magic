using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Interfaces
{
	public interface ICast
	{
		public Component Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(Component source, bool parent);

		public void StopParticles();
		
		public GameObject GetGameObject();
	}
}