using AI.Enums;
using AI.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace AI.ActionModes
{
	public class Patrol : IActionMode
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
			if (target == null || !Owner.WithinRange.SenseDistanceCheck(target, false, false))
			{
				// Reached point, continue to the next one
				if (Owner.Patrolling.HasReachedPoint() == (true, false))
					Owner.Patrolling.GoToNextPoint();
				
				// Target lost, go back to the current point
				if (Owner.AIMode == EAIMode.Action)
				{
					// Don't repeat going to current point if waiting
					if (Owner.Patrolling.WaitUntil > 0f && Time.time < Owner.Patrolling.WaitUntil)
						return;
					
					Owner.Patrolling.GoToCurrentPoint();
					return;
				}
				
				return;
			}

			// Currently on a NavMeshLink, wait for it to complete
			if (Owner.IsOnLink)
				return;
			
			// If stationary, just aim and kill, otherwise chase
			if (((NPCData)Owner.Data).Stationary)
				Owner.KillTarget.AimAndKill(target, true, true, true);
			else
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