using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using UnityEngine;

namespace Combat.Attacks
{
	public class Bind : BaseAttack
	{
		private IAlive alive;
		
		public override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			
			if (alive != null || !AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var targetAlive))
				return;

			alive = targetAlive;
			alive.SetBound(true);

			Target = targetAlive.GetTransform();
		}

		public override void OnTriggersEnabled()
		{
			alive = null;
			base.OnTriggersEnabled();
		}

		public override void OnTriggersDisabled()
		{
			base.OnTriggersDisabled();
			alive?.SetBound(false);
		}
	}
}