using AI.Enums;
using AI.Interfaces;
using AI.Navigation;
using Cysharp.Threading.Tasks;
using Objects.Base;
using Objects.Enums;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.AI;

namespace AI.AIModes
{
	public class Walking : IAIMode
	{
		public NPC Owner { get; set; }

		public float LastEntered { get; private set; }
		public float LastExited { get; private set; }

		public bool IsFlightStuck => nextMoveAllowed > 0f;
		
		private bool jumpingLink;
		
		private float flightStuckTime;
		private float nextMoveAllowed;
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;

			nextMoveAllowed = 0f;
			
			if (owner.ToggleAgent(true))
				Owner.Agent.SetDestination(Owner.Destination);
		}
		
		public void Disabled()
		{
			if (jumpingLink)
				forceFinishJump();

			nextMoveAllowed = 0f;
			
			Owner.IsOnLink = null;
			
			Owner.ToggleAgent(false);
			Owner = null;
			
			LastExited = Time.time;
		}
		
		public void Update()
		{
			var agent = Owner.Agent;
			if (agent.isOnOffMeshLink)
			{
				var data = agent.currentOffMeshLinkData;
				Owner.IsOnLink = data;
				
				// Jump if needed
				if (NavMeshTools.IsJumpLink(data) && !jumpingLink)
				{
					jumpLink().Forget();
					return;
				}
				
				// Either open a door or wait for it to open
				if (NavMeshTools.IsDoorLink(data))
				{
					var action = ((Component)data.owner).GetComponent<NavMeshDoorLink>();
					if (action.Door.State != EDoorState.Open)
					{
						// Not usable and no buttons, likely activated by trigger so just wait for it to open
						if (!action.Door.ObjectData.IsUsable && action.Buttons.Length == 0)
							return;
						
						// Is opening or already has a user, wait
						if (action.IsPartial || action.User != null)
							return;

						// Have the npc open it
						if (action.TryOpen(Owner))
							return;
					}
				}

				// Raise/lower an elevator (or wait for it), get in it and raise/lower it again
				if (NavMeshTools.IsElevatorLink(data))
				{
					var action = ((Component)data.owner).GetComponent<NavMeshElevatorLink>();
					var state = action.Elevator.State;

					var lowerDist = Mathf.Abs(Owner.Destination.y - action.LowerLink.position.y);
					var upperDist = Mathf.Abs(Owner.Destination.y - action.UpperLink.position.y);
					
					var goingDown = lowerDist < 3f;
					var goingUp = upperDist < 3f;

					// Couldn't find what direction the npc is going, try picking lower distance as an alternative
					if (!goingDown && !goingUp)
					{
						if (upperDist > lowerDist)
							goingDown = true;
						else if (lowerDist > upperDist)
							goingUp = true;
					}
					
					if (goingDown || goingUp)
					{
						// On the platform, either stay or step off
						if (action.PlatformUser == Owner)
						{
							if (goingDown && state == EElevatorState.Lowered || goingUp && state == EElevatorState.Elevated)
							{
								// Reached destination, step off the platform
								action.GetOffPlatform();
							}
							else if (!action.IsSteppingOn && !action.IsSteppingOff)
							{
								var ownerTr = Owner.GetTransform();
								
								// Moving to the destination, stay on the platform
								ownerTr.position = action.StepTarget.position + Vector3.up * (agent.baseOffset * ownerTr.localScale.y);
							}
							
							return;
						}

						// Someone else is on the platform, wait
						if (action.PlatformUser != null)
							return;

						if (goingDown && state == EElevatorState.Elevated)
						{
							// Elevator is up, step on the platform and use it
							action.GetOnPlatform(Owner, false);
							return;
						}
						
						if (goingUp && state == EElevatorState.Lowered)
						{
							// Elevator is down, step on the platform and use it
							action.GetOnPlatform(Owner, true);
							return;
						}

						BaseButton useButton = null;

						// Grab the correct button to press
						if (goingDown && state != EElevatorState.Elevated)
							useButton = action.ElevateButton;
						else if (goingUp && state != EElevatorState.Lowered)
							useButton = action.LowerButton;

						// No button, likely activated by trigger so just wait for it to get there
						if (useButton == null)
							return;

						// Is moving or already has a button user, wait
						if (action.IsPartial || action.ButtonUser != null)
							return;

						// Have the npc use the button
						if (action.TryUse(Owner, useButton))
							return;
					}
				}
			}
			else
			{
				Owner.IsOnLink = null;
			}

			if (Owner.Flight == null)
			{
				if (agent.pathPending || !agent.isOnNavMesh || agent.remainingDistance > agent.stoppingDistance || agent.hasPath && agent.velocity.sqrMagnitude != 0f)
					return;
			}
			else
			{
				var time = Time.time;

				if (Owner.AttackTarget != null || Owner.OtherTarget != null)
				{
					// Control taken over for some time because stuck
					if (nextMoveAllowed > 0f)
					{
						if (time >= nextMoveAllowed)
							nextMoveAllowed = 0f;
						else
							return;
					}
				}
				
				// Likely stuck, change move target to something around itself
				if (Owner.Velocity.magnitude < 0.5f)
				{
					flightStuckTime += Time.deltaTime;
					if (flightStuckTime >= 1.5f)
					{
						flightStuckTime = 0f;
						nextMoveAllowed = time + 1.5f;

						Owner.Chase.ResetChaseRange(true);
						Owner.Wandering.WalkRandomly(true);
						return;
					}
				}
				else
				{
					flightStuckTime = 0f;
				}
				
				if (Vector3.Distance(Owner.Body.Rigidbody.position, Owner.Destination) > agent.stoppingDistance)
					return;
			}

			// Reached destination, go back to what was being done earlier
			Owner.ReturnAIMode();
		}

		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
		{
			nextMoveAllowed = 0f;
		}
		
		public void OtherTargetChanged(Component previousOtherTarget, Component newOtherTarget)
		{
			nextMoveAllowed = 0f;
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			if (Owner.Agent.enabled)
				Owner.Agent.SetDestination(newDestination);
		}

		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}

		private void forceFinishJump()
		{
			if (!Owner.IsAlive)
				return;
			
			Owner.Agent.updateRotation = true;
			Owner.Agent.CompleteOffMeshLink();
			jumpingLink = false;
		}
		
		// Borrowed and modified from https://github.com/llamacademy/ai-series-part-2/blob/main/Assets/AgentLinkMover.cs
		private async UniTask jumpLink()
		{
			jumpingLink = true;

			var agent = Owner.Agent;
			var data = agent.currentOffMeshLinkData;
			var transform = Owner.GetTransform();

			agent.updateRotation = false;
			await lookAtLink(data);
			
			var startPos = transform.position;
			var endPos = data.endPos + Vector3.up * (agent.baseOffset * transform.localScale.y);
			
			var normalizedTime = 0.0f;
			while (normalizedTime < 1.0f)
			{
				await UniTask.NextFrame();

				if (Owner == null)
					return;

				if (Owner.AIMode != EAIMode.Walking || !Owner.IsAlive)
				{
					forceFinishJump();
					return;
				}

				if (Owner.Paralyzed)
					continue;
				
				var yOffset = Owner.JumpCurve.Evaluate(normalizedTime);
				transform.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
				
				normalizedTime += Time.deltaTime / Owner.JumpDuration;
			}
			
			agent.CompleteOffMeshLink();
			agent.updateRotation = true;
			
			jumpingLink = false;
		}

		private async UniTask lookAtLink(OffMeshLinkData data)
		{
			var transform = Owner.GetTransform();

			var targetPosition = data.endPos - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);

			var rotationSpeed = ((NPCData)Owner.Data).RotationSpeed;
			
			while (Quaternion.Angle(transform.rotation, targetRotation) > 5f)
			{
				await UniTask.NextFrame();

				if (Owner == null)
					return;

				if (Owner.AIMode != EAIMode.Walking)
				{
					forceFinishJump();
					return;
				}

				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
			}
		}
	}
}