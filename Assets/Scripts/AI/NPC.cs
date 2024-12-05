using System.Collections.Generic;
using AI.ActionModes;
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

		[SerializeField]
		public AnimationCurve JumpCurve;

		[SerializeField]
		public float JumpDuration = 0.75f;

		[SerializeField]
		public EAutoTarget AutoTarget = EAutoTarget.Player;

		[SerializeField]
		public float AutoTargetRange = 25f;

		[SerializeField]
		public float AutoTargetEvery = 0.5f;
		
		public EAIMode AIMode { get; private set; }
		public EActionMode ActionMode { get; private set; }
		public Component Target { get; private set; }
		public Vector3 Destination { get; private set; }
		
		public bool EndActionWithoutTarget { get; private set; }
		
		public readonly Dictionary<EAIMode, IAIMode> AIModes = new ()
		{
			{ EAIMode.Idle, new Idle() },
			{ EAIMode.Walking, new Walking() },
			{ EAIMode.Action, new Action() }
		};
		
		public readonly Dictionary<EActionMode, IActionMode> ActionModes = new ()
		{
			{ EActionMode.None, new None() },
			{ EActionMode.RageTurret, new RageTurret() },
			{ EActionMode.AimingTurret, new AimingTurret() },
			{ EActionMode.ChaseAndKill, new ChaseAndKill() }
		};

		private readonly Dictionary<IAlive, float> targets = new ();

		private float lastAutoTarget;
		
		private EAIMode previousAIMode;
		private EActionMode previousActionMode;
		private Component previousTarget;
		private Vector3 previousDestination;

		#region AI
		
		public void Walk(Vector3 destination)
		{
			if (!IsAlive)
				return;
			
			setDestination(destination);
			setAIMode(EAIMode.Walking);
		}
		
		public void Act(Component target, EActionMode mode, bool endWithoutTarget)
		{
			if (!IsAlive)
				return;

			EndActionWithoutTarget = endWithoutTarget;
			
			setTarget(target);
			setActionMode(mode);
			setAIMode(EAIMode.Action);
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
			if (target == null)
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
				if (EndActionWithoutTarget && Target == null)
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

			Debug.Log($"[NPC {gameObject.name}] Changed AI Mode from {previousAIMode} to {AIMode}");
		}
		
		private void setActionMode(EActionMode mode)
		{
			if (ActionMode == mode)
				return;

			if (mode == EActionMode.None)
				EndActionWithoutTarget = true;
			
			previousActionMode = ActionMode;
			
			ActionModes[ActionMode].Disabled();
			ActionMode = mode;
			ActionModes[ActionMode].Enabled(this);
			
			Debug.Log($"[NPC {gameObject.name}] Changed Action Mode from {previousActionMode} to {ActionMode}");
		}

		private void setTarget(Component target)
		{
			if (Target == target)
				return;
			
			previousTarget = Target;
			Target = target;
			
			ActionModes[ActionMode].TargetChanged(previousTarget, Target);
			AIModes[AIMode].TargetChanged(previousTarget, Target);

			Debug.Log($"[NPC {gameObject.name}] Changed Target from {previousTarget} to {Target}");
		}
		
		private void setDestination(Vector3 destination)
		{
			if (Destination == destination)
				return;
			
			previousDestination = Destination;
			Destination = destination;
			
			ActionModes[ActionMode].DestinationChanged(previousDestination, Destination);
			AIModes[AIMode].DestinationChanged(previousDestination, Destination);

			Debug.Log($"[NPC {gameObject.name}] Changed Destination from {previousDestination} to {Destination}");
		}

		private async UniTask autoTarget()
		{
			while (IsAlive)
			{
				if (AutoTarget == EAutoTarget.None)
					continue;
				
				targets.Clear();

				var pos = transform.position;

				if (AutoTarget.HasFlag(EAutoTarget.Player))
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

				if (AutoTarget.HasFlag(EAutoTarget.NPCs))
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
				
				await UniTask.WaitForSeconds(AutoTargetEvery);
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
			Agent.speed = maximumSpeed;
			base.Spawn(startingHealth, overloadHealth, maximumSpeed);
			
			autoTarget().Forget();
		}

		public override void Kill(object source)
		{
			Agent.enabled = false;
			base.Kill(source);
		}

		#endregion
	}
}