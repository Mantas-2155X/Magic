using AI.Interfaces;
using Combat.Attacks.Base;
using Managers;
using Tools;
using UnityEngine;

namespace Combat.Attacks
{
	public class Bind : BaseAttack
	{
		public IAlive BoundAlive { get; private set; }
		
		public override void OnTriggerEnter(Collider other)
		{
			base.OnTriggerEnter(other);
			
			if (BoundAlive.NotNull() || !AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var targetAlive))
				return;

			BoundAlive = targetAlive;
			BoundAlive.AddSlowSource(ObjectID, 1f, float.MaxValue);

			Target = targetAlive;
		}

		public override void OnTriggersEnabled()
		{
			BoundAlive = null;
			base.OnTriggersEnabled();
		}

		public override void OnTriggersDisabled()
		{
			base.OnTriggersDisabled();

			if (BoundAlive.NotNull())
				BoundAlive.RemoveSlowSource(ObjectID);
		}
	}
}