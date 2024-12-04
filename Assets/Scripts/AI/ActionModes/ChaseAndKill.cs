using AI.Enums;
using AI.Interfaces;
using UnityEngine;

namespace AI.ActionModes
{
	public class ChaseAndKill : IActionMode
	{
		public NPC Owner { get; set; }

		public bool ReturnAfterTargetGone => true;
		
		private float followRange = 25f;
		private float stopAt = 10f;

		private bool currentlyFollowing;
		private float currentStopAt;
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			currentStopAt = stopAt;
		}
		
		public void Disabled()
		{
			Owner = null;
		}
		
		public void Update()
		{
			if (Owner.Target == null)
				return;

			var target = Owner.Target.transform;
			var transform = Owner.transform;
			var targetToOwnerDistance = Vector3.Distance(target.position, transform.position);
			
			// Target too far, give up chasing
			if (targetToOwnerDistance > followRange)
			{
				currentlyFollowing = false;
				return;
			}
			
			currentlyFollowing = true;

			// Try to stop at this distance
			var currentStopTarget = currentStopAt + Owner.Agent.stoppingDistance;
			
			// NPC not within target range, keep walking
			if (targetToOwnerDistance > currentStopTarget)
			{
				// Target within destination range, keep current path
				if (Vector3.Distance(target.position, Owner.Destination) <= currentStopAt + Owner.Agent.stoppingDistance)
				{
					if (Owner.AIMode != EAIMode.Walking)
						Owner.Walk(target.position);

					return;
				}
				
				// Target moved away from destination range, reset the path
				if (currentStopAt < stopAt)
				{
					currentStopAt = stopAt;
					Debug.Log($"[NPC {transform.name}] Resetting ChaseAndKill chase range because target moved away");
				}

				Owner.Walk(target.position);
				return;
			}

			// Within range but can't see the target, reduce the stop range to walk closer to the target
			if (!Owner.HasSightOf(Owner.Target, stopAt + Owner.Agent.stoppingDistance))
			{
				currentStopAt /= 1.2f;
				Debug.Log($"[NPC {transform.name}] Reducing ChaseAndKill chase range to {currentStopAt}");

				if (Owner.AIMode != EAIMode.Walking)
					Owner.Walk(target.position);
				
				return;
			}

			currentStopAt = stopAt;

			// Reached walking range
			if (Owner.AIMode == EAIMode.Walking)
			{
				// Performing jump, stay on walking state
				if (Owner.Agent.isOnOffMeshLink)
					return;
				
				// Go back to action
				Owner.ReturnAIMode();
			}
		}
		
		public void FixedUpdate()
		{
			if (Owner.AIMode != EAIMode.Action || Owner.Target == null || !currentlyFollowing)
				return;

			var target = Owner.Target.transform;
			var transform = Owner.transform;
			var weapon = Owner.Weapon;

			var targetPosition = target.position - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(9f, 12f));
			
			if (Quaternion.Angle(transform.rotation, targetRotation) < 5f)
				weapon?.Attack();
		}
		
		public void TargetChanged(Component previousTarget, Component newTarget)
		{

		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			
		}
	}
}