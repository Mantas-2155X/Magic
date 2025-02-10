using AI.Enums;
using AI.Interfaces;
using Objects.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace AI.ActionModes
{
	public class Carry : IActionMode
	{
		public NPC Owner { get; set; }
		
		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }

		public Vector3 DropAt;

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
			
			// Nothing to carry, return to previous action
			if (target == null || target is not Rigidbody rb)
			{
				owner.ReleaseObject();
				owner.ReturnActionMode();
				return;
			}

			// Go grab the object
			if (owner.Grabbing != target)
			{
				// Release any previous object
				owner.ReleaseObject();
				
				var targetTr = owner.OtherTargetTransform;
				var targetPos = targetTr.position;

				// Stop next to the object
				var stopAt = 2.5f + owner.Agent.stoppingDistance;

				// NPC not within object range, keep walking
				if (Vector3.Distance(ownerPos, targetPos) > stopAt)
				{
					// Object within destination range, keep current path
					if (Vector3.Distance(targetPos, owner.Destination) <= stopAt)
					{
						if (owner.AIMode != EAIMode.Walking)
							owner.Walk(targetPos);

						return;
					}

					// Object moved away from destination range, reset the path
					owner.Walk(targetPos);
					return;
				}

				// Turn towards the object and grab it
				if (!owner.AimAt.AimTowards(targetTr))
					return;

				if (owner.Paralyzed)
					return;

				// Grab object and walk to drop destination
				owner.GrabObject(rb);
			}
			else
			{
				// Stop next to the object
				var stopAt = 2.5f + owner.Agent.stoppingDistance;

				// NPC not within drop off range, keep walking
				if (Vector3.Distance(ownerPos, DropAt) > stopAt)
				{
					// Drop off within destination range, keep current path
					if (Vector3.Distance(DropAt, owner.Destination) <= stopAt)
					{
						if (owner.AIMode != EAIMode.Walking)
							owner.Walk(DropAt);

						return;
					}

					// Drop off moved away from destination range, reset the path
					owner.Walk(DropAt);
					return;
				}
				
				// Drop the object
				owner.ReleaseObject();
				
				// Done carrying it or failed, return to previous action either way
				owner.ReturnActionMode();
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