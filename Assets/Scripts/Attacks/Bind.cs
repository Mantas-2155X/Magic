using AI.Interfaces;
using Attacks.Base;
using UnityEngine;

namespace Attacks
{
	public class Bind : BaseAttack
	{
		private IAlive target;
		private float previousMaxSpeed;
		
		public void OnTriggerEnter(Collider other)
		{
			if (target != null)
				return;
			
			var alive = other.GetComponent<IAlive>();
			if (alive == null || !alive.IsAlive)
				return;

			target = alive;
			previousMaxSpeed = alive.MaximumSpeed;
			GetTransform().position = alive.GetTransform().position + Vector3.down * 1f;
			alive.SetMaxSpeed(0f);
		}

		public override void OnTriggerEnabled()
		{
			target = null;
			previousMaxSpeed = 0f;
			base.OnTriggerEnabled();
		}

		public override void OnTriggerDisabled()
		{
			target?.SetMaxSpeed(previousMaxSpeed);
			base.OnTriggerDisabled();
		}
	}
}