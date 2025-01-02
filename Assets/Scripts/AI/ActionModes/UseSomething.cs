using AI.Enums;
using AI.Interfaces;
using Objects.Interfaces;
using ScriptableObjects;
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
			var ownerPos = owner.GetTransform().position;
			
			var target = owner.OtherTarget;
			var targetPos = owner.OtherTargetTransform.position;

			var senseRange = ((NPCData)owner.Data).SenseRange;
			
			// Nothing to use or target is too far, return to previous action
			if (target == null || Vector3.Distance(targetPos, ownerPos) > senseRange)
			{
				owner.ReturnActionMode();
				return;
			}

			// Stop next to the usable
			var stopAt = 1f + owner.Agent.stoppingDistance;
			
			// NPC not within target range, keep walking
			if (Vector3.Distance(ownerPos, targetPos) > stopAt)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(targetPos, owner.Destination) <= stopAt)
				{
					if (owner.AIMode != EAIMode.Walking)
						owner.Walk(targetPos);

					return;
				}
				
				// Usable moved away from destination range, reset the path
				owner.Walk(targetPos);
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