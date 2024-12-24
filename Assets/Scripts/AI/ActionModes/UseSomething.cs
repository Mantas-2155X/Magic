using AI.Enums;
using AI.Interfaces;
using Objects.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class UseSomething : IActionMode
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
			var owner = Owner;
			
			var target = owner.OtherTarget;
			var targetTr = owner.OtherTargetTransform;
			
			// Nothing to use, return to previous action
			if (target == null)
			{
				owner.ReturnActionMode();
				return;
			}

			// Stop next to the usable
			var stopAt = 1f + owner.Agent.stoppingDistance;
			
			// NPC not within target range, keep walking
			if (Vector3.Distance(owner.GetTransform().position, targetTr.position) > stopAt)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(targetTr.position, owner.Destination) <= stopAt)
				{
					if (owner.AIMode != EAIMode.Walking)
						owner.Walk(targetTr.position);

					return;
				}
				
				// Usable moved away from destination range, reset the path
				owner.Walk(targetTr.position);
				return;
			}

			// Use it if possible
			if (target is IObject obj)
				obj.Use(owner);
			
			// Done using it or failed, return to previous action either way
			owner.ReturnActionMode();
		}
		
		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
		{
			
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