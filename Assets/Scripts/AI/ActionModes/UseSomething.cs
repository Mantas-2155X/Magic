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
		
		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }

		public Vector3? WalkAfterwards;

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
			var owner = Owner;
			var ownerPos = owner.GetTransform().position;
			
			var target = owner.OtherTarget;
			
			// Nothing to use, return to previous action
			if (target == null)
			{
				owner.ReturnActionMode();
				return;
			}
			
			var targetPos = owner.OtherTargetTransform.position;

			// Target is too far, return to previous action
			if (Vector3.Distance(targetPos, ownerPos) > ((NPCData)owner.Data).SenseRange)
			{
				owner.ReturnActionMode();
				return;
			}

			// Stop next to the usable
			var stopAt = 2.5f + owner.Agent.stoppingDistance;
			
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

			// In case this was a button to open a door, continue to the destination
			if (WalkAfterwards != null)
			{
				owner.Walk(WalkAfterwards.Value);
				WalkAfterwards = null;
			}
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