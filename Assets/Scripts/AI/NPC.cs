//#define DEBUG_NPC

using System;
using System.Collections.Generic;
using AI.ActionModes;
using AI.ActionModes.Shared;
using AI.AIModes;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using Managers;
using UnityEngine;
using UnityEngine.AI;
using Action = AI.AIModes.Action;

namespace AI
{
	public class NPC : BaseAlive
	{
		[SerializeField]
		public NavMeshAgent Agent;

		#region Jump

		[SerializeField]
		public AnimationCurve JumpCurve;

		[SerializeField]
		public float JumpDuration = 0.75f;

		#endregion
		
		#region Action Mode Parameters

		/// <summary>
		/// Distance around itself that the npc can sense targets in
		/// (WithinRange)
		/// </summary>
		[SerializeField]
		public float SenseRange = 25f;

		/// <summary>
		/// Maximum distance from the npc to the target via a direct raycast determining if the target can be seen
		/// (HasSight)
		/// </summary>
		[SerializeField]
		public float SightRange = 15f;

		/// <summary>
		/// Distance between the npc and the target at which the npc counts the target to be in range and stops going closer
		/// (Chase)
		/// </summary>
		[SerializeField]
		public float ChaseRange = 10f;

		/// <summary>
		/// Distance between the npc and the patrol point at which the npc is considered to have reached the point
		/// (Patrol)
		/// </summary>
		[SerializeField]
		public float PatrolReachRange = 0.5f;
		
		/// <summary>
		/// Min-Max range of how fast can the npc rotate when performing an action
		/// (AimAt, Spin)
		/// </summary>
		[SerializeField]
		public float RotationSpeed = 480f;

		/// <summary>
		/// Maximum look angle between the npc and the target which the npc deems accurate enough
		/// (AimAt)
		/// </summary>
		[SerializeField]
		public float AimAngle = 5f;

		/// <summary>
		/// Wander every x seconds after the last walking state finished
		/// (Wander)
		/// </summary>
		[SerializeField]
		public float WanderEvery = 1f;
		
		/// <summary>
		/// How far around the npc communications are received and sent
		/// </summary>
		[SerializeField]
		public float CommunicateRange = 5f;
		
		#endregion
		
		public EAIMode AIMode { get; private set; }
		public EActionMode ActionMode { get; private set; }
		
		public IAIMode AIModeObj { get; private set; }
		public IActionMode ActionModeObj { get; private set; }

		public Component Target { get; private set; }
		public Transform TargetTransform { get; private set; }
		
		public Vector3 Destination { get; private set; }

		public AimAt AimAt { get; private set; }
		public Chase Chase { get; private set; }
		public Wander Wander { get; private set; }
		public Patrol Patrol { get; private set; }
		public HasSight HasSight { get; private set; }
		public WithinRange WithinRange { get; private set; }
		
		public readonly Dictionary<EAIMode, IAIMode> AIModes = new (new EAIModeComparer())
		{
			{ EAIMode.Idle, new Idle() },
			{ EAIMode.Walking, new Walking() },
			{ EAIMode.Action, new Action() }
		};
		
		public readonly Dictionary<EActionMode, IActionMode> ActionModes = new (new EActionModeComparer())
		{
			{ EActionMode.None, new None() },
			{ EActionMode.WanderAggressively, new WanderAggressively() },
			{ EActionMode.PatrolAggressively, new PatrolAggressively() },
			{ EActionMode.WaitAggressively, new WaitAggressively() }
		};
		
		private EAIMode previousAIMode;
		private EActionMode previousActionMode;
		private Component previousTarget;
		private Vector3 previousDestination;

		#region AI
		
		#region Action Modes

		public void WanderAggressively(Component target)
		{
			if (!IsAlive)
				return;
			
			setTarget(target);
			setActionMode(EActionMode.WanderAggressively);
			setAIMode(EAIMode.Action);
		}

		public void PatrolAggressively(Component target, List<Vector3> points, int startAt = -1)
		{
			if (!IsAlive)
				return;
			
			Patrol.SetPoints(points, startAt);
			
			setTarget(target);
			setActionMode(EActionMode.PatrolAggressively);
			setAIMode(EAIMode.Action);
		}
		
		public void WaitAggressively(Component target)
		{
			if (!IsAlive)
				return;
			
			setTarget(target);
			setActionMode(EActionMode.WaitAggressively);
			setAIMode(EAIMode.Action);
		}

		#endregion
		
		public void Walk(Vector3 destination)
		{
			if (!IsAlive)
				return;
			
			setDestination(destination);
			setAIMode(EAIMode.Walking);
		}

		public void Chill()
		{
			if (!IsAlive)
				return;

			setActionMode(EActionMode.None);
			setAIMode(EAIMode.Idle);
		}

		public void SendCommunication(ECommunication type, object data, NPC communicator)
		{
			var npcs = AIManager.Instance.NPCs;
			var pos = GetTransform().position;

			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc == this)
					continue;

				// Don't communicate back to the communicator to prevent infinite comms bouncing
				if (npc == communicator)
					continue;
				
				// Don't communicate with your target
				if (npc == Target)
					continue;
				
				var distance = Vector3.Distance(pos, npc.GetTransform().position);
				if (distance > CommunicateRange)
					continue;
				
				npc.ReceiveCommunication(type, this, data);
			}
		}
		
		public void ReceiveCommunication(ECommunication type, NPC source, object data)
		{
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Received communication {type} from {source.GetGameObject().name} with data {data}");
#endif
			
			switch (type)
			{
				case ECommunication.TargetAcquired:
					setTarget((Component)data, source);
					break;
				default:
					throw new NotImplementedException();
			}
		}
		
		public void AssignTarget(Component target)
		{
			if (!IsAlive)
				return;
			
			setTarget(target);
		}

		public void ReturnAIMode()
		{
			if (!IsAlive)
				return;
			
			setAIMode(previousAIMode);
		}
		
		public void ReturnActionMode()
		{
			if (!IsAlive)
				return;
			
			setActionMode(previousActionMode);
		}
		
		public void ReturnTarget()
		{
			if (!IsAlive)
				return;
			
			setTarget(previousTarget);
		}
		
		public void ReturnDestination()
		{
			if (!IsAlive)
				return;
			
			setDestination(previousDestination);
		}
		
		private void setAIMode(EAIMode mode)
		{
			if (AIMode == mode)
				return;
			
			Weapon?.CancelCasting();
			previousAIMode = AIMode;
			
			AIModeObj?.Disabled();
			AIMode = mode;
			AIModeObj = AIModes[mode];
			AIModeObj.Enabled(this);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed AI Mode from {previousAIMode} to {AIMode}");
#endif
		}
		
		private void setActionMode(EActionMode mode)
		{
			if (ActionMode == mode)
				return;
			
			Weapon?.CancelCasting();
			previousActionMode = ActionMode;
			
			ActionModeObj?.Disabled();
			ActionMode = mode;
			ActionModeObj = ActionModes[mode];
			ActionModeObj.Enabled(this);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Action Mode from {previousActionMode} to {ActionMode}");
#endif
		}

		private void setTarget(Component target, NPC communicator = null)
		{
			if (Target == target)
				return;
			
			// Don't target alives of the same relationship group
			if (Target is IAlive alive && alive.RelationshipGroup == RelationshipGroup)
				return;
			
			Weapon?.CancelCasting();
			previousTarget = Target;
			Target = target;
			TargetTransform = target == null ? null : target.GetComponent<Transform>();
			
			ActionModeObj.TargetChanged(previousTarget, Target);
			AIModeObj.TargetChanged(previousTarget, Target);

			if (Target != null)
				SendCommunication(ECommunication.TargetAcquired, Target, communicator);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Target from {previousTarget} to {Target}");
#endif
		}
		
		private void setDestination(Vector3 destination)
		{
			if (Destination == destination)
				return;
			
			previousDestination = Destination;
			Destination = destination;
			
			ActionModeObj.DestinationChanged(previousDestination, Destination);
			AIModeObj.DestinationChanged(previousDestination, Destination);
		
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Destination from {previousDestination} to {Destination}");
#endif
		}
		
		#endregion

		#region MonoBehaviour

		public void Update()
		{
			if (!IsAlive)
				return;
			
			if (AIMode == EAIMode.Walking && Agent.hasPath)
				Body.ShouldSway = true;
			
			ActionModeObj.Update();
			AIModeObj.Update();
		}

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => !IsWalking ? Agent.speed : Agent.velocity.magnitude;
		
		public override bool IsWalking => Agent.hasPath;

		public override void SetMaxSpeed(float maximumSpeed)
		{
			base.SetMaxSpeed(maximumSpeed);
			Agent.speed = maximumSpeed;
		}
		
		public override void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed, int relationshipGroup)
		{
			AIModeObj = AIModes[AIMode];
			ActionModeObj = ActionModes[ActionMode];

			AimAt = new AimAt(this);
			Chase = new Chase(this);
			Wander = new Wander(this);
			Patrol = new Patrol(this);
			HasSight = new HasSight(this);
			WithinRange = new WithinRange(this);
			
			base.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, maximumSpeed, relationshipGroup);
		}

		public override void Kill(object source)
		{
			Agent.enabled = false;
			base.Kill(source);
		}
		
		public override bool IsGrounded()
		{
			return true;
		}

		#endregion
	}
}