using AI;
using AI.ActionModes;
using AI.Enums;
using Newtonsoft.Json;
using ScriptableObjects;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class NPCState
	{
		[JsonProperty]
		public EAIMode AIMode;
		
		[JsonProperty]
		public EActionMode ActionMode;

		[JsonProperty]
		public Vector3 Destination;

		[JsonProperty]
		public string AttackTargetObjectID;
		
		[JsonProperty]
		public string OtherTargetObjectID;
		
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
		public string UseObjectID;
		
		[JsonProperty]
		public Vector3? UseWalkAfterwards;

		#endregion

		#region Carry

		[JsonProperty]
		public string CarryObjectID;
		
		[JsonProperty]
		public Vector3 CarryDropAt;

		#endregion
		
		#region Self-Destruct

		[JsonProperty]
		public bool SelfDestructed;

		[JsonProperty]
		public float SelfDestructElapsed;
		
		#endregion

		public static NPCState Read(NPC npc)
		{
			if (npc == null)
				return null;

			var npcData = (NPCData)npc.Data;
			var state = new NPCState();

			state.AIMode = npc.AIMode;
			state.ActionMode = npc.ActionMode;
			
			state.Destination = npc.Destination;
			
			// attacktarget
			// othertarget
			
			#region Patrol

			state.PatrolPath = npc.Patrolling.CurrentPathData != null ? npc.Patrolling.CurrentPathData.Name : null;
			state.PatrolStartAt = npc.Patrolling.CurrentPoint;
			state.PatrolAlreadyWaited = npc.Patrolling.WaitOnArrival ? npc.Patrolling.WaitUntil - Time.time : 0f;

			#endregion
			
			#region Use

			// useobjectid
			state.UseWalkAfterwards = ((Use)npc.ActionModes[EActionMode.Use]).WalkAfterwards;

			#endregion
			
			#region Carry
			
			// carryobjectid
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

			npc.SetState(state.SelfDestructed, state.SelfDestructElapsed);
		}
	}
}