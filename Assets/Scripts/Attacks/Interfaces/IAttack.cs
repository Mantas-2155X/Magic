using ScriptableObjects;
using UnityEngine;

namespace Attacks.Interfaces
{
	public interface IAttack
	{
		public AttackData AttackData { get; set; }

		public Component Source { get; }

		public ParticleSystem System { get; }
		public Collider Trigger { get; }
		
		public void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach);

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}