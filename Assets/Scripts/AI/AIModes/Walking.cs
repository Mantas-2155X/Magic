using AI.Enums;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
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
		
		private bool jumpingLink;
		
		public void Enabled(NPC owner)
		{
			Owner = owner;
			LastEntered = Time.time;

			if (owner.ToggleAgent(true))
				Owner.Agent.SetDestination(Owner.Destination);
		}
		
		public void Disabled()
		{
			if (jumpingLink)
				forceFinishJump();
			
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
						
						// Is opening or already has an user, wait
						if (action.IsPartial || action.User != null)
							return;

						// Have the npc open it
						if (action.TryOpen(Owner))
							return;
					}
				}
			}
			
			if (agent.pathPending || !agent.isOnNavMesh || agent.remainingDistance > agent.stoppingDistance || agent.hasPath && agent.velocity.sqrMagnitude != 0f)
				return;
			
			// Reached destination, go back to what was being done earlier
			Owner.ReturnAIMode();
		}

		public void AttackTargetChanged(Component previousAttackTarget, Component newAttackTarget)
		{
			
		}
		
		public void OtherTargetChanged(Component previousOtherTarget, Component newOtherTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
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
			var endPos = data.endPos + Vector3.up * agent.baseOffset;
			
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