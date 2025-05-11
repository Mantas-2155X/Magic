using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Attacks.Interfaces
{
	public interface IAttack : ISaveable
	{
		public AttackData AttackData { get; }

		public IIdentifiable Source { get; }

		public ParticleSystem System { get; }
		public Collider[] Triggers { get; }
		
		public IIdentifiable Target { get; set; }

		public float CreatedTime { get; }

		public void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f);

		public IAlive GetAlive();
	}
}