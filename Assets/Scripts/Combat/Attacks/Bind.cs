using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using UnityEngine;

namespace Combat.Attacks
{
	public class Bind : BaseAttack
	{
		private IAlive target;
		private float previousMaxSpeed;
		
		public void OnTriggerEnter(Collider other)
		{
			if (target != null || !AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
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