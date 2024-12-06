//#define DEBUG_NPC

using System.Collections.Generic;
using AI.ActionModes;
using AI.ActionModes.Shared;
using AI.AIModes;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
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
		
		#region Auto Target

		[SerializeField]
		public EAutoTarget AutoTarget = EAutoTarget.None;

		[SerializeField]
		public float AutoTargetRange = 25f;

		[SerializeField]
		public float AutoTargetEvery = 0.5f;

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
		public float SightRange = 11f;

		/// <summary>
		/// Distance between the npc and the target at which the npc counts the target to be in range and stops going closer
		/// (Chase)
		/// </summary>
		[SerializeField]
		public float ChaseRange = 10f;

		/// <summary>
		/// Min-Max range of how many degrees per step can the npc rotate when performing an action
		/// (AimAt, Spin)
		/// </summary>
		[SerializeField]
		public Vector2 RotationStep = new (9f, 12f);

		/// <summary>
		/// Maximum look angle between the npc and the target which the npc deems accurate enough
		/// (AimAt)
		/// </summary>
		[SerializeField]
		public float AimAngle = 5f;
		
		#endregion
		
		public EAIMode AIMode { get; private set; }
		public EActionMode ActionMode { get; private set; }
		public Component Target { get; private set; }
		public Vector3 Destination { get; private set; }
		public bool AimLimited { get; private set; }
		public bool ActWithoutTarget { get; private set; }

		public Spin Spin { get; private set; }
		public AimAt AimAt { get; private set; }
		public Chase Chase { get; private set; }
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
			{ EActionMode.RageTurret, new RageTurret() },
			{ EActionMode.AimingTurret, new AimingTurret() },
			{ EActionMode.FindAndKill, new FindAndKill() }
		};
		
		private EAIMode previousAIMode;
		private EActionMode previousActionMode;
		private Component previousTarget;
		private Vector3 previousDestination;

		private readonly Dictionary<IAlive, float> targets = new ();

		#region AI
		
		#region Action Modes

		public void RageTurret(Component target = null, bool aimLimited = true, bool actWithoutTarget = true)
		{
			if (!IsAlive)
				return;

			AimLimited = aimLimited;
			ActWithoutTarget = actWithoutTarget;
			
			setTarget(target);
			setActionMode(EActionMode.RageTurret);
			setAIMode(EAIMode.Action);
		}
		
		public void AimingTurret(Component target, bool aimLimited = false, bool actWithoutTarget = false)
		{
			if (!IsAlive)
				return;
			
			AimLimited = aimLimited;
			ActWithoutTarget = actWithoutTarget;
			
			setTarget(target);
			setActionMode(EActionMode.AimingTurret);
			setAIMode(EAIMode.Action);
		}

		public void FindAndKill(Component target, bool aimLimited = false, bool actWithoutTarget = false)
		{
			if (!IsAlive)
				return;
			
			AimLimited = aimLimited;
			ActWithoutTarget = actWithoutTarget;
			
			setTarget(target);
			setActionMode(EActionMode.FindAndKill);
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
		
		public bool HasSightOf(Component target, float maxRange)
		{
			if (!IsAlive || target == null)
				return false;
			
			var ownerTr = transform;
			var direction = (target.transform.position - ownerTr.position).normalized;
			var ray = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);

			if (!Physics.Raycast(ray, out var hit, maxRange, ~LayerMaskTools.Mask2))
				return false;
			
			var rb = hit.rigidbody;
			if (rb == null)
				return false;

			var components = rb.GetComponents<Component>();
			foreach (var component in components)
			{
				if (component != target)
					continue;

				return true;
			}

			return false;
		}
		
		public void ReturnAIMode(bool resetAction = false)
		{
			if (!IsAlive)
				return;

			if (resetAction)
				setActionMode(EActionMode.None);
			
			if (previousAIMode == EAIMode.Action)
			{
				// Protect from an infinite loop of walk-action when the target is gone and the action mode returns after target death
				if (!ActWithoutTarget && Target == null)
				{
					setActionMode(EActionMode.None);
					setAIMode(EAIMode.Idle);
					return;
				}
			}
			
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
		
		private async UniTask autoTarget()
		{
			while (IsAlive)
			{
				await UniTask.WaitForSeconds(AutoTargetEvery);

				if (AutoTarget == EAutoTarget.None)
					continue;
				
				targets.Clear();

				var pos = transform.position;

				if ((AutoTarget & EAutoTarget.Player) != 0)
				{
					var player = AIManager.Instance.Player;
					if (player != null && player.IsAlive)
					{
						var dist = Vector3.Distance(player.transform.position, pos);
						if (dist < AutoTargetRange)
							targets.Add(player, dist);
					}
				}
				else if (Target is Player)
				{
					setTarget(null);
				}

				if ((AutoTarget & EAutoTarget.NPCs) != 0)
				{
					var npcs = AIManager.Instance.NPCs;

					for (var i = 0; i < npcs.Count; i++)
					{
						var npc = npcs[i];
						if (npc == null || !npc.IsAlive || npc == this)
							continue;

						var dist = Vector3.Distance(npc.transform.position, pos);
						if (dist < AutoTargetRange)
							targets.Add(npc, dist);
					}
				}
				else if (Target is NPC)
				{
					setTarget(null);
				}

				if (Target != null && Vector3.Distance(Target.transform.position, pos) >= AutoTargetRange)
					setTarget(null);
				
				var closestDistance = Mathf.Infinity;
				IAlive closestAlive = null;

				foreach (var target in targets)
				{
					if (target.Value > closestDistance)
						continue;
					
					closestAlive = target.Key;
					closestDistance = target.Value;
				}

				if (closestAlive != null)
					setTarget((Component)closestAlive);
			}
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

		public void FixedUpdate()
		{
			if (!IsAlive)
				return;

			ActionModes[ActionMode].FixedUpdate();
			AIModes[AIMode].FixedUpdate();
		}

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => !IsWalking ? Agent.speed : Agent.velocity.magnitude;
		
		public override bool IsWalking => Agent.hasPath;

		public override void Spawn(int startingHealth, int overloadHealth, float maximumSpeed)
		{ 
			Spin = new Spin(this);
			AimAt = new AimAt(this);
			Chase = new Chase(this);
			HasSight = new HasSight(this);
			WithinRange = new WithinRange(this);

			Agent.speed = maximumSpeed;
			
			base.Spawn(startingHealth, overloadHealth, maximumSpeed);
			
			autoTarget().Forget();
		}

		public override void Kill(object source)
		{
			Agent.enabled = false;
			base.Kill(source);
		}
		
		public override bool IsGrounded()
		{
			if (Agent.enabled)
				return true;
			
			// TODO: implement simple raycast, collisions are too heavy for lots of NPCs
			return false;
		}

		#endregion
	}
}