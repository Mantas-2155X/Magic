using AI.Enums;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
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

			toggleAgent(true);
			
			Owner.Agent.SetDestination(Owner.Destination);
		}
		
		public void Disabled()
		{
			if (jumpingLink)
				forceFinishJump();
			
			toggleAgent(false);
			Owner = null;
			
			LastExited = Time.time;
		}
		
		public void Update()
		{
			var agent = Owner.Agent;
			if (agent.isOnOffMeshLink && !jumpingLink)
			{
				jumpLink().Forget();
				return;
			}
			
			if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance || agent.hasPath && agent.velocity.sqrMagnitude != 0f)
				return;
			
			// Reached destination, go back to what was being done earlier
			Owner.ReturnAIMode();
		}

		public void TargetChanged(Component previousTarget, Component newTarget)
		{
			
		}

		public void DestinationChanged(Vector3 previousDestination, Vector3 newDestination)
		{
			Owner.Agent.SetDestination(newDestination);
		}
		
		private void toggleAgent(bool state)
		{
			if (Owner.Agent.enabled == state)
				return;
			
			Owner.Agent.enabled = state;
			Owner.Body.Rigidbody.isKinematic = state;
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

			agent.updateRotation = false;
			await lookAtLink(data);

			var agentTr = agent.transform;
			
			var startPos = agentTr.position;
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
				agentTr.position = Vector3.Lerp(startPos, endPos, normalizedTime) + yOffset * Vector3.up;
				
				normalizedTime += Time.deltaTime / Owner.JumpDuration;
			}
			
			agent.CompleteOffMeshLink();
			agent.updateRotation = true;
			
			jumpingLink = false;
		}

		private async UniTask lookAtLink(OffMeshLinkData data)
		{
			var transform = Owner.transform;
			
			var targetPosition = data.endPos - transform.position;
			targetPosition.y = 0;
			
			var targetRotation = Quaternion.LookRotation(targetPosition);
			
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
				
				transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, Random.Range(Owner.RotationSpeed.x, Owner.RotationSpeed.y) * Time.deltaTime);
			}
		}
	}
}