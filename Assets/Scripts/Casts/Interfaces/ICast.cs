using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Interfaces
{
	public interface ICast
	{
		public IWeapon Source { get; }

		public ParticleSystem System { get; }
		
		public void Spawn(IWeapon source, bool parent);

		public void StopParticles();
		
		public GameObject GetGameObject();
	}
}