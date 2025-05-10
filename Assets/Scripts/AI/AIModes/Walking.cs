using AI.Enums;
using AI.Interfaces;
using AI.Navigation;
using Cysharp.Threading.Tasks;
using Objects.Base;
using Objects.Enums;
using ScriptableObjects;
using State.Interfaces;
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
		
		private bool jumpingLink;
		private float flightStuckTime;
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;
			
			if (!Owner.Agent.IsNavMesh)
			{
				Owner.Agent.SetDestination(Owner.Destination);
				return;
			}

			if (!Owner.ToggleAgent(true))
				return;

			Owner.Agent.SetDestination(Owner.Destination);
		}
		
		public void Disabled()
		{
			if (jumpingLink)
				forceFinishJump();
			
			Owner.IsOnLink = null;
			
			Owner.ToggleAgent(false);
			Owner = null;
			
			LastExited = Time.time;
		}
		
		public void Update()
		{
			var agent = Owner.Agent;
			if (agent.IsOnOffMeshLink)
			{
				var data = agent.CurrentOffMeshLinkData;
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
								ownerTr.position = action.StepTarget.position + Vector3.up * (agent.BaseOffset * ownerTr.localScale.y);
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

			if (agent.PathPending || !agent.IsOnNavMesh || agent.RemainingDistance > agent.StoppingDistance || agent.HasPath && agent.Velocity.sqrMagnitude != 0f)
				return;

			// Reached destination, go back to what was being done earlier
			Owner.ReturnAIMode();
		}

		public void AttackTargetChanged(IIdentifiable previousAttackTarget, IIdentifiable newAttackTarget)
		{
			
		}
		
		public void OtherTargetChanged(IIdentifiable previousOtherTarget, IIdentifiable newOtherTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			if (!Owner.Agent.IsOnNavMesh)
				return;

			if (!Owner.Agent.IsNavMesh)
			{
				Owner.Agent.SetDestination(newDestination);
				return;
			}

			if (!Owner.Agent.NavMeshAgent.enabled)
				return;

			Owner.Agent.SetDestination(newDestination);
		}

		public void CommunicationReceived(ECommunication type, NPC source, object data)
		{
			
		}

		private void forceFinishJump()
		{
			if (!Owner.IsAlive)
				return;
			
			Owner.Agent.UpdateRotation = true;
			Owner.Agent.CompleteOffMeshLink();
			jumpingLink = false;
		}
		
		// Borrowed and modified from https://github.com/llamacademy/ai-series-part-2/blob/main/Assets/AgentLinkMover.cs
		private async UniTask jumpLink()
		{
			jumpingLink = true;

			var agent = Owner.Agent;
			var data = agent.CurrentOffMeshLinkData;
			var transform = Owner.GetTransform();

			agent.UpdateRotation = false;
			await lookAtLink(data);
			
			var startPos = transform.position;
			var endPos = data.endPos + Vector3.up * (agent.BaseOffset * transform.localScale.y);
			
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
			agent.UpdateRotation = true;
			
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