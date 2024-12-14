using System;
using AI.Interfaces;
using UnityEngine;

namespace Attacks.Interfaces
{
	public interface IAttack
	{
		public ParticleSystem System { get; }
		
		public Collider Trigger { get; }

		public float EnableTriggerAfter { get; }
		public float DisableTriggerAfter { get; }
		
		public IAlive Owner { get; }

		public Type Type { get; }

		public void Spawn(IAlive owner, Vector3 position, Vector3 angles, bool parent);
	}
}