using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class WanderAggressively : IActionMode
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

			// Target does not exist or is further than the sense range, wander until one is close enough
			if (target == null || !Owner.WithinRange.SenseDistanceCheck(target))
			{
				if (Owner.AIMode != EAIMode.Walking)
					Owner.Wander.WalkRandomly(false);
				
				return;
			}

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
		
		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}
	}
}