using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class WaitAggressively : IActionMode
	{
		public NPC Owner { get; set; }
		
		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }

		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;
		}
		
		public void Disabled()
		{
			Owner = null;
			LastExited = Time.time;
		}
		
		public void Update()
		{
			var target = Owner.AttackTargetTransform;

			// Target does not exist or is further than the sense range, keep waiting until one is enough
			if (target == null || !Owner.WithinRange.SenseDistanceCheck(target))
				return;

			// Currently on a NavMeshLink, wait for it to complete
			if (Owner.IsOnLink)
				return;
			
			Owner.Chase.ChaseAndKill(target);
		}
		
		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
		{
			Owner.Chase.ResetChaseRange(true);
		}
		
		public void OtherTargetChanged(Component previousOtherTarget, Component newOtherTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
		
		public void AggressiveChanged(bool previousAggressive, bool newAggressive)
		{
			
		}
		
		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}
	}
}