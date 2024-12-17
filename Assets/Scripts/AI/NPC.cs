//#define DEBUG_NPC

using System.Collections.Generic;
using AI.ActionModes;
using AI.ActionModes.Shared;
using AI.AIModes;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AI;

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
		/// Min-Max range of how fast can the npc rotate when performing an action
		/// (AimAt, Spin)
		/// </summary>
		[SerializeField]
		public Vector2 RotationSpeed = new (720f, 780f);

		/// <summary>
		/// Maximum look angle between the npc and the target which the npc deems accurate enough
		/// (AimAt)
		/// </summary>
		[SerializeField]
		public float AimAngle = 5f;

		/// <summary>
		/// Wander every x seconds after the last walking state finised
		/// (Wander)
		/// </summary>
		[SerializeField]
		public float WanderEvery = 1f;
		
		#endregion
		
		public EAIMode AIMode { get; private set; }
		public EActionMode ActionMode { get; private set; }
		public Component Target { get; private set; }
		public Vector3 Destination { get; private set; }

		public Spin Spin { get; private set; }
		public AimAt AimAt { get; private set; }
		public Chase Chase { get; private set; }
		public Wander Wander { get; private set; }
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
			{ EActionMode.WanderAggressively, new WanderAggressively() }
		};
		
		private EAIMode previousAIMode;
		private EActionMode previousActionMode;
		private Component previousTarget;
		private Vector3 previousDestination;

		private Transform thisTr;

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
		
		public bool HasSightOf(Transform target, float maxRange)
		{
			if (!IsAlive || target == null)
				return false;
			
			var direction = (target.position - thisTr.position).normalized;
			var ray = new Ray(thisTr.position + thisTr.up * 0.5f, direction);

			if (!Physics.Raycast(ray, out var hit, maxRange, ~LayerMaskTools.GetMask()))
				return false;
			
			return hit.collider.transform == target;
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
			
			AIModes[AIMode].Disabled();
			AIMode = mode;
			AIModes[AIMode].Enabled(this);

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
			
			ActionModes[ActionMode].Disabled();
			ActionMode = mode;
			ActionModes[ActionMode].Enabled(this);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Action Mode from {previousActionMode} to {ActionMode}");
#endif
		}

		private void setTarget(Component target)
		{
			if (Target == target)
				return;
			
			Weapon?.CancelCasting();
			previousTarget = Target;
			Target = target;
			
			ActionModes[ActionMode].TargetChanged(previousTarget, Target);
			AIModes[AIMode].TargetChanged(previousTarget, Target);

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
			
			ActionModes[ActionMode].DestinationChanged(previousDestination, Destination);
			AIModes[AIMode].DestinationChanged(previousDestination, Destination);
		
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

			if (Target != null && Target is IAlive alive && !alive.IsAlive)
				setTarget(null);
			
			if (Agent.hasPath)
				Body.ShouldSway = true;
			
			ActionModes[ActionMode].Update();
			AIModes[AIMode].Update();
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
		
		public override void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed)
		{
			thisTr = transform;
			
			Spin = new Spin(this);
			AimAt = new AimAt(this);
			Chase = new Chase(this);
			Wander = new Wander(this);
			HasSight = new HasSight(this);
			WithinRange = new WithinRange(this);
			
			base.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, maximumSpeed);
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