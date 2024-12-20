using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class WaitAggressively : IActionMode
	{
		public NPC Owner { get; set; }
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			var target = Owner.TargetTransform;

			// Target does not exist or is further than the sense range, keep waiting until one is enough
			if (target == null || !Owner.WithinRange.SenseDistanceCheck(target))
				return;

			// Target within sense range, chase until it is reached
			if (!Owner.Chase.ChaseTarget(target))
				return;

			// Reached target, stop walking and go into action
			if (Owner.AIMode == EAIMode.Walking)
			{
				Owner.ReturnAIMode();
				return;
			}
			
			if (Owner.AIMode == EAIMode.Action)
			{
				// Turn towards the target and aim
				if (!Owner.AimAt.AimTowardsTarget(target))
					return;
			
				Owner.Weapon?.StartCasting();
			}
		}
		
		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			Owner.Chase.ResetChaseRange();
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
		
		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}
	}
}