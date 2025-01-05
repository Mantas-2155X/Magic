using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class PatrolAggressively : IActionMode
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

			// Target does not exist or is further than the sense range, patrol until one is close enough
			if (target == null || !Owner.WithinRange.SenseDistanceCheck(target))
			{
				// Target lost, go back to the current point
				if (Owner.AIMode == EAIMode.Action)
				{
					Owner.Patrol.GoToCurrentPoint();
					return;
				}

				// Reached point, continue to the next one
				if (Owner.Patrol.HasReachedPoint())
					Owner.Patrol.GoToNextPoint();
				
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