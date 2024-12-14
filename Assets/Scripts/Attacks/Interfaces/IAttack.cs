using AI.Interfaces;
using UnityEngine;

namespace Attacks.Interfaces
{
	public interface IAttack
	{
		public IAlive Owner { get; }

		public ParticleSystem System { get; }
		public Collider Trigger { get; }

		public float EnableTriggerAfter { get; }
		public float DisableTriggerAfter { get; }
		
		public void Spawn(IAlive owner, Vector3 position, Quaternion angles, bool parent);
	}
}