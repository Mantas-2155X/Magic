using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using Tools;
using UnityEngine;

namespace Combat.Attacks
{
	public class Bind : BaseAttack
	{
		private IAlive alive;
		
		public override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			
			if (!alive.IsNull() || !AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var targetAlive))
				return;

			alive = targetAlive;
			alive.AddSlowSource(ObjectID, 1f, float.MaxValue);

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

			if (!alive.IsNull())
				alive.RemoveSlowSource(ObjectID);
		}
	}
}