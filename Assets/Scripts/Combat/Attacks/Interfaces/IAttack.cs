using AI.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace Combat.Attacks.Interfaces
{
	public interface IAttack : IIdentifiable
	{
		public AttackData AttackData { get; }

		public Component Source { get; }

		public ParticleSystem System { get; }
		public Collider[] Triggers { get; }
		
		public Transform Target { get; set; }

		public void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach);

		public IAlive GetAlive();

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}