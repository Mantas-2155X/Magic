using UnityEngine;
using Weapons.Interfaces;

namespace Casts.Interfaces
{
	public interface ICast
	{
		public ParticleSystem System { get; }
		
		public IWeapon Source { get; }
		
		public void Spawn(IWeapon source, bool parent);

		public void StopParticles();
		
		public GameObject GetGameObject();
	}
}