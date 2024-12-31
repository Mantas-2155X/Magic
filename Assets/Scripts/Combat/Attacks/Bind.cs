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
		
		public override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			
			if (target != null || !AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				return;

			target = alive;
			previousMaxSpeed = alive.MaximumSpeed;
			GetTransform().position = alive.GetTransform().position + Vector3.down * 1f;
			alive.SetMaxSpeed(0f);
		}

		public override void OnTriggersEnabled()
		{
			target = null;
			previousMaxSpeed = 0f;
			base.OnTriggersEnabled();
		}

		public override void OnTriggersDisabled()
		{
			target?.SetMaxSpeed(previousMaxSpeed);
			base.OnTriggersDisabled();
		}
	}
}