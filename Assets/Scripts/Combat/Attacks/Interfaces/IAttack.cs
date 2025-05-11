using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Attacks.Interfaces
{
	public interface IAttack : IIdentifiable
	{
		public AttackData AttackData { get; }

		public IIdentifiable Source { get; }

		public ParticleSystem System { get; }
		public Collider[] Triggers { get; }
		
		public IIdentifiable Target { get; set; }

		public void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach);

		public IAlive GetAlive();
	}
}