using AI;
using AI.ActionModes;
using AI.Enums;
using Newtonsoft.Json;
using ScriptableObjects;
using Tools;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class NPCState
	{
		[JsonProperty]
		public EAIMode AIMode;
		
		[JsonProperty]
		public EAIMode PreviousAIMode;
		
		[JsonProperty]
		public EActionMode ActionMode;
		
		[JsonProperty]
		public EActionMode PreviousActionMode;

		[JsonProperty]
		public Vector3 Destination;
		
		[JsonProperty]
		public Vector3 PreviousDestination;

		[JsonProperty]
		public string AttackTargetObjectID;
		
		[JsonProperty]
		public string PreviousAttackTargetObjectID;
		
		[JsonProperty]
		public string OtherTargetObjectID;
		
		[JsonProperty]
		public string PreviousOtherTargetObjectID;
		
		[JsonProperty]
		public Vector3? AgentPosition;
		
		[JsonProperty]
		public bool SelfDestructed;

		[JsonProperty]
		public float SelfDestructElapsed;

		// switch cast cooldown
		// chase interrupt timer
		// off mesh link travelling
		
		#region Patrol

		[JsonProperty]
		public string PatrolPath;
		
		[JsonProperty]
		public int PatrolStartAt;

		[JsonProperty]
		public float PatrolAlreadyWaited;

		#endregion

		#region Use
		
		[JsonProperty]
		public Vector3? UseWalkAfterwards;

		#endregion

		#region Carry
		
		[JsonProperty]
		public Vector3 CarryDropAt;

		#endregion

		public static NPCState Read(NPC npc)
		{
			if (npc == null)
				return null;

			var npcData = (NPCData)npc.Data;
			var state = new NPCState();

			state.AIMode = npc.AIMode;
			state.PreviousAIMode = npc.PreviousAIMode;
			
			state.ActionMode = npc.ActionMode;
			state.PreviousActionMode = npc.PreviousActionMode;
			
			state.Destination = npc.Destination;
			state.PreviousDestination = npc.PreviousDestination;
			
			state.AttackTargetObjectID = npc.AttackTarget.NotNull() ? npc.AttackTarget.ObjectID : null;
			state.PreviousAttackTargetObjectID = npc.PreviousAttackTarget.NotNull() ? npc.PreviousAttackTarget.ObjectID : null;
			
			state.OtherTargetObjectID = npc.OtherTarget.NotNull() ? npc.OtherTarget.ObjectID : null;
			state.PreviousOtherTargetObjectID = npc.PreviousOtherTarget.NotNull() ? npc.PreviousOtherTarget.ObjectID : null;
			
			state.AgentPosition = npc.Agent.NavMeshAgent != null && npc.Agent.NavMeshAgent.enabled ? npc.Agent.NavMeshAgent.nextPosition : null;
			
			#region Patrol

			state.PatrolPath = npc.Patrolling.CurrentPathData != null ? npc.Patrolling.CurrentPathData.Name : null;
			state.PatrolStartAt = npc.Patrolling.CurrentPoint;
			state.PatrolAlreadyWaited = npc.Patrolling.WaitOnArrival ? npc.Patrolling.WaitUntil - Time.time : 0f;

			#endregion
			
			#region Use

			state.UseWalkAfterwards = ((Use)npc.ActionModes[EActionMode.Use]).WalkAfterwards;

			#endregion
			
			#region Carry
			
			state.CarryDropAt = ((Carry)npc.ActionModes[EActionMode.Carry]).DropAt;

			#endregion
			
			#region Self-Destruct

			state.SelfDestructed = npc.SelfDestructed;
			state.SelfDestructElapsed = npcData.CanSelfDestruct ? Time.time - npc.SelfDestructStart : 0f;
			
			#endregion
			
			return state;
		}

		public static void Apply(NPC npc, NPCState state)
		{
			if (npc == null)
				return;

			npc.SetSelfDestructState(state.SelfDestructed, state.SelfDestructElapsed);
		}
	}
}