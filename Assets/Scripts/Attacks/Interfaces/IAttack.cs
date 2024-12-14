using UnityEngine;

namespace Attacks.Interfaces
{
	public interface IAttack
	{
		public Component Source { get; }

		public ParticleSystem System { get; }
		public Collider Trigger { get; }

		public float EnableTriggerAfter { get; }
		public float DisableTriggerAfter { get; }
		
		public bool Attach { get; }
		
		public void Spawn(Component source, Vector3 position, Quaternion angles, Transform attach);
	}
}