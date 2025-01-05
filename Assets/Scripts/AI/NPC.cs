//#define DEBUG_NPC

using System.Collections.Generic;
using AI.ActionModes;
using AI.ActionModes.Shared;
using AI.AIModes;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using Managers;
using ScriptableObjects;
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
		
		public EAIMode AIMode { get; private set; }
		public EActionMode ActionMode { get; private set; }
		
		public IAIMode AIModeObj { get; private set; }
		public IActionMode ActionModeObj { get; private set; }

		public Component AttackTarget { get; private set; }
		public Transform AttackTargetTransform { get; private set; }
		
		public Component OtherTarget { get; private set; }
		public Transform OtherTargetTransform { get; private set; }
		
		public Vector3 Destination { get; private set; }

		public float SwitchCastCooldown { get; set; }
		
		public AimAt AimAt { get; private set; }
		public Chase Chase { get; private set; }
		public Wander Wander { get; private set; }
		public Patrol Patrol { get; private set; }
		public HasSight HasSight { get; private set; }
		public WithinRange WithinRange { get; private set; }
		public LowResources LowResources { get; private set; }
		
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
			{ EActionMode.WaitAggressively, new WaitAggressively() },
			{ EActionMode.UseSomething, new UseSomething() }
		};
		
		private EAIMode previousAIMode;
		private EActionMode previousActionMode;
		private Component previousAttackTarget;
		private Component previousOtherTarget;
		private Vector3 previousDestination;

		#region AI
		
		#region Action Modes

		public void WanderAggressively()
		{
			if (!IsAlive)
				return;
			
			setActionMode(EActionMode.WanderAggressively);
			setAIMode(EAIMode.Action);
		}

		public void PatrolAggressively(List<Vector3> points, int startAt = -1)
		{
			if (!IsAlive)
				return;
			
			Patrol.SetPoints(points, startAt);
			
			setActionMode(EActionMode.PatrolAggressively);
			setAIMode(EAIMode.Action);
		}
		
		public void WaitAggressively()
		{
			if (!IsAlive)
				return;
			
			setActionMode(EActionMode.WaitAggressively);
			setAIMode(EAIMode.Action);
		}

		public void UseSomething(Component target)
		{
			if (!IsAlive)
				return;
			
			setOtherTarget(target);
			setActionMode(EActionMode.UseSomething);
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
		
		public void SendCommunication(ECommunication type, object data)
		{
			var npcs = AIManager.Instance.NPCs;
			var pos = GetTransform().position;

			var range = ((NPCData)Data).CommunicateRange;
			
			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc == this)
					continue;

				var distance = Vector3.Distance(pos, npc.GetTransform().position);
				if (distance > range)
					continue;
				
				npc.ReceiveCommunication(type, this, data);
			}
		}
		
		public void ReceiveCommunication(ECommunication type, NPC source, object data)
		{
			ActionModeObj.CommunicationReceived(type, source, data);
			AIModeObj.CommunicationReceived(type, source, data);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Received communication {type} from {source.GetGameObject().name} with data {data}");
#endif
		}
		
		public bool ToggleAgent(bool state)
		{
			var agent = Agent;
			if (agent.enabled == state)
				return true;
			
			agent.enabled = state;
			Body.Rigidbody.isKinematic = state;

			if (!state || agent.isOnNavMesh)
				return true;

			Debug.LogWarning($"[{name}] Agent is outside of navmesh, killing");
			Kill(this);

			return false;
		}
		
		public void AssignAttackTarget(Component target)
		{
			if (!IsAlive)
				return;
			
			setAttackTarget(target);
		}
		
		public void AssignOtherTarget(Component target)
		{
			if (!IsAlive)
				return;
			
			setOtherTarget(target);
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
		
		public void ReturnAttackTarget()
		{
			if (!IsAlive)
				return;
			
			setAttackTarget(previousAttackTarget);
		}
		
		public void ReturnOtherTarget()
		{
			if (!IsAlive)
				return;
			
			setOtherTarget(previousOtherTarget);
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
			
			if (Spell != null)
				Spell.CancelCasting();
			
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
			
			if (Spell != null)
				Spell.CancelCasting();
			
			previousActionMode = ActionMode;
			
			ActionModeObj?.Disabled();
			ActionMode = mode;
			ActionModeObj = ActionModes[mode];
			ActionModeObj.Enabled(this);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Action Mode from {previousActionMode} to {ActionMode}");
#endif
		}

		private void setAttackTarget(Component target)
		{
			if (AttackTarget == target)
				return;
			
			if (Spell != null)
				Spell.CancelCasting();
			
			previousAttackTarget = AttackTarget;
			AttackTarget = target;
			AttackTargetTransform = target == null ? null : target.GetComponent<Transform>();
			
			ActionModeObj.AttackTargetChanged(previousAttackTarget, AttackTarget);
			AIModeObj.AttackTargetChanged(previousAttackTarget, AttackTarget);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Attack Target from {previousAttackTarget} to {AttackTarget}");
#endif
		}
		
		private void setOtherTarget(Component target)
		{
			if (OtherTarget == target)
				return;
			
			previousOtherTarget = OtherTarget;
			OtherTarget = target;
			OtherTargetTransform = target == null ? null : target.GetComponent<Transform>();
			
			ActionModeObj.OtherTargetChanged(previousOtherTarget, OtherTarget);
			AIModeObj.OtherTargetChanged(previousOtherTarget, OtherTarget);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Other Target from {previousOtherTarget} to {OtherTarget}");
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
		
		private void handleAttackTarget()
		{
			// Don't change target when already casting
			if (IsCasting)
				return;

			var forgetCurrent = false;
			
			// Attack target exists, check for validity
			if (AttackTargetTransform != null)
			{
				// Must be within sense range
				if (WithinRange.SenseDistanceCheck(AttackTargetTransform))
				{
					// Make sure it can be seen
					if (HasSight.SightCheck(AttackTargetTransform, true))
					{
						// Must be alive and have a different relationship
						if (AttackTarget is IAlive alive && alive.IsAlive && alive.RelationshipGroup != RelationshipGroup)
						{
							// Valid, keep it
							return;
						}
					}
				}
				else
				{
					// Outside of sense range, forget it if one isn't found
					forgetCurrent = true;
				}
			}
			
			var position = GetTransform().position;
			var alivesMap = AIManager.Instance.AlivesColliderMap;

			BaseAlive closestAlive = null;
			var closestDistance = float.PositiveInfinity;

			foreach (var pair in alivesMap)
			{
				var alive = (BaseAlive)pair.Value;
				
				if (this == alive || !alive.IsAlive)
					continue;

				// Don't target same relationship
				if (RelationshipGroup == alive.RelationshipGroup)
					continue;

				var aliveTransform = alive.GetTransform();
				
				// Make sure its within sense range and can be seen
				if (!WithinRange.SenseDistanceCheck(aliveTransform) || !HasSight.SightCheck(aliveTransform, true))
					continue;
				
				var distance = Vector3.Distance(position, aliveTransform.position);
				if (distance >= closestDistance)
					continue;
					
				closestDistance = distance;
				closestAlive = alive;
			}

			if (closestAlive != null)
				setAttackTarget(closestAlive);
			else if (forgetCurrent)
				setAttackTarget(null);
		}
		
		#endregion

		#region MonoBehaviour

		public void Update()
		{
			if (!IsAlive)
				return;
			
			if (AIMode == EAIMode.Walking && Agent.hasPath)
				Body.ShouldSway = true;
			
			handleAttackTarget();
			
			ActionModeObj.Update();
			AIModeObj.Update();
		}

		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => !IsWalking ? Agent.speed : Agent.velocity.magnitude;
		
		public override bool IsWalking => Agent.hasPath;

		public override void SetBound(bool value)
		{
			base.SetBound(value);
			Agent.speed = value ? 0f : Data.Speed;
		}
		
		public override void SelectSpell(SpellData data)
		{
			var previousSpell = Spell;
			
			base.SelectSpell(data);
			
			// Apply variation to add a bit of irregularity to switch-cast-switch behavior
			if (Spell != previousSpell)
				SwitchCastCooldown = Time.time + Random.Range(0f, ((NPCData)Data).SpellSwitchCastCooldown);
		}
		
		public override void Spawn(AliveData data, int relationshipGroup)
		{
			AIModeObj = AIModes[AIMode];
			ActionModeObj = ActionModes[ActionMode];

			AimAt = new AimAt(this);
			Chase = new Chase(this);
			Wander = new Wander(this);
			Patrol = new Patrol(this);
			HasSight = new HasSight(this);
			WithinRange = new WithinRange(this);
			LowResources = new LowResources(this);
			
			Agent.speed = data.Speed;
			
			base.Spawn(data, relationshipGroup);
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