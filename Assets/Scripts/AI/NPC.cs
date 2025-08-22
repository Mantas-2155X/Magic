//#define DEBUG_NPC

using System;
using System.Collections.Generic;
using AI.ActionModes;
using AI.ActionModes.Shared;
using AI.AIModes;
using AI.Base;
using AI.Enums;
using AI.Interfaces;
using AI.PathFinding;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ScriptableObjects;
using State;
using State.Enums;
using State.Interfaces;
using Tools;
using UnityEngine;
using UnityEngine.AI;
using Action = AI.AIModes.Action;
using Idle = AI.ActionModes.Idle;
using Patrol = AI.ActionModes.Patrol;
using Random = UnityEngine.Random;

namespace AI
{
	public class NPC : BaseAlive
	{
		[SerializeField]
		public AgentCompat Agent;

		[NonSerialized]
		public string ParentSpawner;
		
		public bool SelfDestructed { get; set; }
		public float SelfDestructStart { get; set; }

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

		public IIdentifiable AttackTarget { get; private set; }
		public Transform AttackTargetTransform { get; private set; }
		
		public IIdentifiable OtherTarget { get; private set; }
		public Transform OtherTargetTransform { get; private set; }
		
		public Vector3 Destination { get; private set; }

		public EAIMode PreviousAIMode { get; private set; }
		public EActionMode PreviousActionMode { get; private set; }
		public IIdentifiable PreviousAttackTarget { get; private set; }
		public IIdentifiable PreviousOtherTarget { get; private set; }
		public Vector3 PreviousDestination { get; private set; }
		
		public float SwitchCastCooldown { get; set; }
		
		public AimAt AimAt { get; private set; }
		public Chase Chase { get; private set; }
		public Wandering Wandering { get; private set; }
		public Patrolling Patrolling { get; private set; }
		public HasSight HasSight { get; private set; }
		public WithinRange WithinRange { get; private set; }
		public LowResources LowResources { get; private set; }
		public KillTarget KillTarget { get; private set; }

		public Vector3 Velocity => IsWalking ? Agent.Velocity : Body.Rigidbody.linearVelocity;
		
		private OffMeshLinkData? isOnLink;
		public OffMeshLinkData? IsOnLink
		{
			get => isOnLink;
			set
			{
				var prev = isOnLink;
				isOnLink = value;
				
				// No link change
				if (prev == null && isOnLink == null)
					return;

				// Lost link
				if (prev != null && isOnLink == null)
				{
					ShrinkObject(false);
					return;
				}

				// Gained link
				if (prev == null && isOnLink != null)
				{
					ShrinkObject(NavMeshTools.IsElevatorLink(isOnLink.Value));
					return;
				}
				
				// Changed link
				if (prev!.Value.owner != isOnLink.Value.owner)
				{
					ShrinkObject(NavMeshTools.IsElevatorLink(isOnLink.Value));
				}
			}
		}
		
		public readonly Dictionary<EAIMode, IAIMode> AIModes = new (new EAIModeComparer())
		{
			{ EAIMode.Idle, new AIModes.Idle() },
			{ EAIMode.Walking, new Walking() },
			{ EAIMode.Action, new Action() }
		};
		
		public readonly Dictionary<EActionMode, IActionMode> ActionModes = new (new EActionModeComparer())
		{
			{ EActionMode.None, new None() },
			{ EActionMode.Wander, new Wander() },
			{ EActionMode.Patrol, new Patrol() },
			{ EActionMode.Idle, new Idle() },
			{ EActionMode.Use, new Use() },
			{ EActionMode.Carry, new Carry() }
		};
		
		#region AI

		#region Action Modes

		public void Wander()
		{
			if (!IsAlive || ((NPCData)Data).Stationary)
				return;
			
			setActionMode(EActionMode.Wander);
			setAIMode(EAIMode.Action);
		}

		public void Patrol(PathData pathData, int startAt = -1, float waitSkipAhead = 0f)
		{
			if (!IsAlive || ((NPCData)Data).Stationary)
				return;
			
			Patrolling.SetPath(pathData, startAt);
			
			setActionMode(EActionMode.Patrol);
			setAIMode(EAIMode.Action);
			
			if (waitSkipAhead <= 0f)
				return;
			
			Patrolling.WaitUntil -= waitSkipAhead;
		}
		
		public void Idle()
		{
			if (!IsAlive)
				return;
			
			setActionMode(EActionMode.Idle);
			setAIMode(EAIMode.Action);
		}

		public void Use(IIdentifiable target, Vector3? walkAfterwards = null)
		{
			if (!IsAlive || ((NPCData)Data).Stationary)
				return;

			var actionMode = (Use)ActionModes[EActionMode.Use];
			actionMode.WalkAfterwards = walkAfterwards;
			
			setOtherTarget(target);
			setActionMode(EActionMode.Use);
			setAIMode(EAIMode.Action);
		}
		
		public void Carry(IIdentifiable target, Vector3 dropAt)
		{
			var data = (NPCData)Data;
			if (!IsAlive || data.Stationary || !data.CanGrab)
				return;
			
			var actionMode = (Carry)ActionModes[EActionMode.Carry];
			actionMode.DropAt = dropAt;

			setOtherTarget(target);
			setActionMode(EActionMode.Carry);
			setAIMode(EAIMode.Action);
		}

		#endregion
		
		public void Walk(Vector3 destination)
		{
			if (!IsAlive || ((NPCData)Data).Stationary)
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
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Sending communication {type} with data {data}");
#endif
			
			var npcs = AIManager.Instance.NPCs;
			var pos = GetTransform().position;

			var range = ((NPCData)Data).CommunicateRange;
			
			for (var i = 0; i < npcs.Count; i++)
			{
				var npc = npcs[i];
				if (!npc.IsAlive || npc == this || npc.RelationshipGroup != RelationshipGroup)
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
			if (!Agent.IsNavMesh)
				return false;
			
			var agent = Agent.NavMeshAgent;
			if (agent.enabled == state)
				return true;
			
			agent.enabled = state;
			
			// Walking agents aren't using physics
			Body.Rigidbody.isKinematic = state;

			// Disabling the agent, so return false
			if (!state)
				return false;

			// Enabling agent, return true if on nav mesh
			if (agent.isOnNavMesh)
				return true;

			// Enabling agent, not on navmesh - kill
			Debug.LogWarning($"[NPC {gameObject.name}] Agent is outside of navmesh, killing");
			Kill(this);

			return false;
		}
		
		public void AssignAttackTarget(IIdentifiable target)
		{
			if (!IsAlive)
				return;
			
			setAttackTarget(target);
		}
		
		public void AssignOtherTarget(IIdentifiable target)
		{
			if (!IsAlive)
				return;
			
			setOtherTarget(target);
		}

		public void ReturnAIMode()
		{
			if (!IsAlive)
				return;
			
			setAIMode(PreviousAIMode);
		}
		
		public void ReturnActionMode()
		{
			if (!IsAlive)
				return;
			
			setActionMode(PreviousActionMode);
		}
		
		public void ReturnAttackTarget()
		{
			if (!IsAlive)
				return;
			
			setAttackTarget(PreviousAttackTarget);
		}
		
		public void ReturnOtherTarget()
		{
			if (!IsAlive)
				return;
			
			setOtherTarget(PreviousOtherTarget);
		}
		
		public void ReturnDestination()
		{
			if (!IsAlive)
				return;
			
			setDestination(PreviousDestination);
		}
		
		private void setAIMode(EAIMode mode)
		{
			if (AIMode == mode)
				return;
			
			if (Spell.NotNull())
				Spell.CancelCasting();
			
			PreviousAIMode = AIMode;
			
			AIModeObj?.Disabled();
			AIMode = mode;
			AIModeObj = AIModes[mode];
			AIModeObj.Enabled(this);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed AI Mode from {PreviousAIMode} to {AIMode}");
#endif
		}
		
		private void setActionMode(EActionMode mode)
		{
			if (ActionMode == mode)
				return;
			
			if (Spell.NotNull())
				Spell.CancelCasting();
			
			PreviousActionMode = ActionMode;
			
			ActionModeObj?.Disabled();
			ActionMode = mode;
			ActionModeObj = ActionModes[mode];
			ActionModeObj.Enabled(this);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Action Mode from {PreviousActionMode} to {ActionMode}");
#endif
		}

		private void setAttackTarget(IIdentifiable target)
		{
			if (AttackTarget == target)
				return;
			
			if (Spell.NotNull())
				Spell.CancelCasting();
			
			PreviousAttackTarget = AttackTarget;
			AttackTarget = target;
			AttackTargetTransform = target.IsNull() ? null : target.GetTransform();
			
			ActionModeObj.AttackTargetChanged(PreviousAttackTarget, AttackTarget);
			AIModeObj.AttackTargetChanged(PreviousAttackTarget, AttackTarget);

			if (AttackTarget.NotNull())
				SendCommunication(ECommunication.AttackTargetFound, AttackTarget);
			else
				SendCommunication(ECommunication.AttackTargetLost, null);

#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Attack Target from {PreviousAttackTarget} to {AttackTarget}");
#endif
		}
		
		private void setOtherTarget(IIdentifiable target)
		{
			if (OtherTarget == target)
				return;
			
			PreviousOtherTarget = OtherTarget;
			OtherTarget = target;
			OtherTargetTransform = target.IsNull() ? null : target.GetTransform();
			
			ActionModeObj.OtherTargetChanged(PreviousOtherTarget, OtherTarget);
			AIModeObj.OtherTargetChanged(PreviousOtherTarget, OtherTarget);

			if (OtherTarget.NotNull())
				SendCommunication(ECommunication.OtherTargetFound, OtherTarget);
			else
				SendCommunication(ECommunication.OtherTargetLost, null);
			
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Other Target from {PreviousOtherTarget} to {OtherTarget}");
#endif
		}
		
		private void setDestination(Vector3 destination)
		{
			if (Destination == destination)
				return;
			
			PreviousDestination = Destination;
			Destination = destination;
			
			ActionModeObj.DestinationChanged(PreviousDestination, Destination);
			AIModeObj.DestinationChanged(PreviousDestination, Destination);
		
#if DEBUG_NPC
			Debug.Log($"[NPC {gameObject.name}] Changed Destination from {PreviousDestination} to {Destination}");
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
				if (WithinRange.SenseDistanceCheck(AttackTargetTransform, false, false))
				{
					// Make sure it can be seen
					if (HasSight.SightCheck(AttackTargetTransform, false))
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

			// Don't look for new targets if not aggressive
			if (((NPCData)Data).TargetMode != ETargetMode.Aggressive)
			{
				if (forgetCurrent)
					setAttackTarget(null);
				
				return;
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
				
				// Make sure it's within sense (and spot) range, field of view and can be seen
				if (!WithinRange.SenseDistanceCheck(aliveTransform, true, false) || !WithinRange.FieldOfViewCheck(aliveTransform) || !HasSight.SightCheck(aliveTransform, false))
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

		#region Identify / SaveLoad
		
		public override bool ExternallySpawned { get => string.IsNullOrEmpty(ParentSpawner); set { } }

		public override ELoadType LoadType => ExternallySpawned ? ELoadType.Create : ELoadType.Modify;

		public override ELoadTiming LoadTiming => ExternallySpawned ? ELoadTiming.Normal : ELoadTiming.Alives;
		
		public override JObject GetCreation()
		{
			var createData = new CreateData()
			{
				Name = Data.Name,
				States = GetModifications()
			};

			return JObject.FromObject(createData);
		}

		public static ISaveable ApplyCreation(Tuple<string, JObject> data)
		{
			var createData = data.Item2.ToObject<CreateData>();
			
			var obj = AIManager.Instance.CreateNPC(Vector3.zero, Vector3.zero, (NPCData)ObjectManager.Instance.GetData<AliveData>(createData.Name), null, 0, false);
			obj.ObjectID = data.Item1;

			try
			{
				obj.ApplyModifications(createData.States);
			}
			catch (Exception e)
			{
				Debug.LogError($"[NPC] Failed loading created object state for {obj.name} ({obj.ObjectID}), {e}");
			}

			return obj;
		}
		
		public override Dictionary<string, JObject> GetModifications()
		{
			var dict = base.GetModifications();
			dict[typeof(NPC).ToString()] = JObject.FromObject(new NPCState(this));
			
			return dict;
		}

		public override void ApplyModifications(Dictionary<string, JObject> data)
		{
			base.ApplyModifications(data);
			
			if (data.TryGetValue(typeof(NPC).ToString(), out var npcState) && npcState != null)
				npcState.ToObject<NPCState>().Apply(this);
		}

		public void SetAIState(
			EAIMode aiMode, EAIMode previousAIMode,
			EActionMode actionMode, EActionMode previousActionMode,
			Vector3 destination, Vector3 previousDestination,
			string attackTargetObjectID, string previousAttackTargetObjectID,
			string otherTargetObjectID, string previousOtherTargetObjectID,
			Vector3? agentPosition,
			string patrolPath, int patrolStartAt, float patrolAlreadyWaited,
			Vector3? useWalkAfterwards,
			Vector3 carryDropAt,
			float switchCastCooldown,
			float chaseInterruptTimer, float chaseInterruptDuration)
		{
			if (!IsAlive)
				return;
			
			var navMeshAgent = Agent.NavMeshAgent;
			
			if (agentPosition != null && navMeshAgent != null && navMeshAgent.enabled)
				navMeshAgent.Warp(agentPosition.Value);
			
			var attackTarget = StateManager.Instance.GetRegisteredObject(attackTargetObjectID);
			var otherTarget = StateManager.Instance.GetRegisteredObject(otherTargetObjectID);
			
			var previousAttackTarget = StateManager.Instance.GetRegisteredObject(previousAttackTargetObjectID);
			var previousOtherTarget = StateManager.Instance.GetRegisteredObject(previousOtherTargetObjectID);

			AIMode = previousAIMode;
			ActionMode = previousActionMode;

			Destination = previousDestination;
			
			AttackTarget = previousAttackTarget;
			OtherTarget = previousOtherTarget;

			Chase.InterruptTimer = chaseInterruptTimer;
			
			if (chaseInterruptDuration > 0f)
				Chase.InterruptUntil = Time.time + chaseInterruptDuration;
			
			if (switchCastCooldown > 0f)
				SwitchCastCooldown = Time.time + switchCastCooldown;
			
			if (actionMode == EActionMode.None)
			{
				setAttackTarget(attackTarget);
				setOtherTarget(otherTarget);
			}
			else if (actionMode == EActionMode.Wander)
			{
				setAttackTarget(attackTarget);
				setOtherTarget(otherTarget);
				Wander();
			}
			else if (actionMode == EActionMode.Patrol)
			{
				setAttackTarget(attackTarget);
				setOtherTarget(otherTarget);
				Patrol(ObjectManager.Instance.GetData<PathData>(patrolPath), patrolStartAt, patrolAlreadyWaited);
			}
			else if (actionMode == EActionMode.Idle)
			{
				setAttackTarget(attackTarget);
				setOtherTarget(otherTarget);
				Idle();
			}
			else if (actionMode == EActionMode.Use)
			{
				setAttackTarget(attackTarget);
				Use(otherTarget, useWalkAfterwards);
			}
			else if (actionMode == EActionMode.Carry)
			{
				setAttackTarget(attackTarget);
				Carry(otherTarget, carryDropAt);
			}
			
			if (aiMode == EAIMode.Idle)
			{
				setDestination(destination);
			}
			else if (aiMode == EAIMode.Walking)
			{
				Walk(destination);
			}
			else if (aiMode == EAIMode.Action)
			{
				setDestination(destination);
			}
			
			if (agentPosition != null && navMeshAgent != null && navMeshAgent.enabled)
				navMeshAgent.Warp(agentPosition.Value);
		}
		
		public void SetSelfDestructState(bool selfDestructed, float selfDestructElapsed)
		{
			if (selfDestructed)
			{
				SelfDestructed = true;
				return;
			}
			
			SelfDestructStart -= selfDestructElapsed;
		}

		#endregion

		#region MonoBehaviour

		public override void Update()
		{
			base.Update();
			
			if (PauseManager.IsPaused)
				return;
			
			if (!IsAlive)
				return;
			
			if (AIMode == EAIMode.Walking && Agent.HasPath)
				Body.ShouldSway = true;
			
			handleAttackTarget();
			
			// If low on resources, see if there's any spell that can be casted
			LowResources.UseResourceSpellIfNeeded();

			ActionModeObj.Update();
			AIModeObj.Update();

			var time = Time.time;

			var npcData = (NPCData)Data;
			if (npcData.CanChaseInterrupt && AttackTarget.NotNull() && AIMode == EAIMode.Action && ActionMode is EActionMode.Idle or EActionMode.Patrol or EActionMode.Wander)
			{
				if (Chase.InterruptUntil < time)
				{
					Chase.InterruptTimer += Time.deltaTime;

					// Wait some time before interrupting
					if (Chase.InterruptTimer >= npcData.ChaseInterruptEvery)
					{
						Chase.InterruptTimer = 0f;
					
						// Interrupt for some time allowing walking
						Chase.InterruptUntil = time + npcData.ChaseInterruptDuration;
						Chase.ResetChaseRange(true);
					
						Wandering.WalkRandomly(true, npcData.ChaseInterruptDistance);
					}
				}
				else
				{
					// Already reached interrupt destination, cancel it early
					Chase.InterruptUntil = time;
				}
			}
			
			if (!npcData.CanSelfDestruct || SelfDestructed)
				return;

			if (time < SelfDestructStart + npcData.SelfDestructAfter)
				return;

			SelfDestructed = true;

			var tr = GetTransform();
			ObjectManager.Instance.CreateAttack(npcData.SelfDestructAttack, this, tr.position, Vector3.zero, this);
		}

#if UNITY_EDITOR
		public void OnDrawGizmos()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawLine(transform.position, Destination);
		}
#endif
		
		#endregion
		
		#region IAlive
		
		public override float CurrentSpeed => !IsWalking ? Agent.Speed : Agent.Velocity.magnitude;
		
		public override bool IsWalking => Agent.HasPath;

		public override void AddSlowSource(string objID, float amount, float duration)
		{
			base.AddSlowSource(objID, amount, duration);
			updateAgentSpeed();
		}
		public override void RemoveSlowSource(string objID)
		{
			base.RemoveSlowSource(objID);
			updateAgentSpeed();
		}
		public override void ClearSlowSources()
		{
			base.ClearSlowSources();
			updateAgentSpeed();
		}
		
		public override void AddParalyzeSource(string objID, float duration)
		{
			base.AddParalyzeSource(objID, duration);
			updateAgentSpeed();
		}
		public override void RemoveParalyzeSource(string objID)
		{
			base.RemoveParalyzeSource(objID);
			updateAgentSpeed();
		}
		public override void ClearParalyzeSources()
		{
			base.ClearParalyzeSources();
			updateAgentSpeed();
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
			Wandering = new Wandering(this);
			Patrolling = new Patrolling(this);
			HasSight = new HasSight(this);
			WithinRange = new WithinRange(this);
			LowResources = new LowResources(this);
			KillTarget = new KillTarget(this);

			var npcData = (NPCData)data;

			Agent.Speed = data.Speed;
			Agent.AngularSpeed = npcData.RotationSpeed;

			if (npcData.CanSelfDestruct)
				SelfDestructStart = Time.time;
			
			base.Spawn(data, relationshipGroup);
			
			SendCommunication(ECommunication.Spawned, null);
		}

		public override void Kill(object source, bool killSilently = false)
		{
			SendCommunication(ECommunication.Died, source);
			
			if (Agent.IsNavMesh)
				Agent.NavMeshAgent.enabled = false;
			
			if (Agent.HasFlight)
				Destroy(Agent.Flight);
			
			base.Kill(source, killSilently);
		}
		
		public override bool IsGrounded()
		{
			return true;
		}

		private void updateAgentSpeed()
		{
			if (Paralyzed)
			{
				Agent.Speed = 0f;
				return;
			}
			
			if (SlowSources.Count == 0)
			{
				Agent.Speed = Data.Speed;
				return;
			}

			var maximum = 0f;

			foreach (var pair in SlowSources)
			{
				if (pair.Value.Item1 <= maximum)
					continue;
				
				maximum = pair.Value.Item1;
			}
			
			Agent.Speed = Data.Speed - (Data.Speed * maximum);
		}
		
		#endregion
		
		[JsonObject]
		public class NPCState : IState
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

			[JsonProperty]
			public Vector3? FlightMovementTarget;

			[JsonProperty]
			public float SwitchCastCooldown;

			[JsonProperty]
			public float ChaseInterruptTimer;
		
			[JsonProperty]
			public float ChaseInterruptDuration;
		
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
			
			public NPCState() { }
			
			public NPCState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not NPC npc)
					return;

				var npcData = (NPCData)npc.Data;
				var time = Time.time;
				
				AIMode = npc.AIMode;
				PreviousAIMode = npc.PreviousAIMode;
				
				ActionMode = npc.ActionMode;
				PreviousActionMode = npc.PreviousActionMode;
				
				Destination = npc.Destination;
				PreviousDestination = npc.PreviousDestination;
				
				AttackTargetObjectID = npc.AttackTarget.NotNull() ? npc.AttackTarget.ObjectID : null;
				PreviousAttackTargetObjectID = npc.PreviousAttackTarget.NotNull() ? npc.PreviousAttackTarget.ObjectID : null;
				
				OtherTargetObjectID = npc.OtherTarget.NotNull() ? npc.OtherTarget.ObjectID : null;
				PreviousOtherTargetObjectID = npc.PreviousOtherTarget.NotNull() ? npc.PreviousOtherTarget.ObjectID : null;
				
				AgentPosition = npc.Agent.NavMeshAgent != null && npc.Agent.NavMeshAgent.enabled ? npc.Agent.NavMeshAgent.nextPosition : null;

				SwitchCastCooldown = npc.SwitchCastCooldown > 0f && time < npc.SwitchCastCooldown ? npc.SwitchCastCooldown - time : 0f;

				ChaseInterruptTimer = npc.Chase.InterruptTimer;
				ChaseInterruptDuration = time < npc.Chase.InterruptUntil ? npc.Chase.InterruptUntil - Time.time : 0f;
				
				#region Patrol

				PatrolPath = npc.Patrolling.CurrentPathData != null ? npc.Patrolling.CurrentPathData.Name : null;
				PatrolStartAt = npc.Patrolling.CurrentPoint;
				PatrolAlreadyWaited = npc.Patrolling.CurrentPathData != null && npc.Patrolling.WaitOnArrival ? npc.Patrolling.WaitUntil - time : 0f;

				#endregion
				
				#region Use

				UseWalkAfterwards = ((Use)npc.ActionModes[EActionMode.Use]).WalkAfterwards;

				#endregion
				
				#region Carry
				
				CarryDropAt = ((Carry)npc.ActionModes[EActionMode.Carry]).DropAt;

				#endregion
				
				#region Self-Destruct

				SelfDestructed = npc.SelfDestructed;
				SelfDestructElapsed = npcData.CanSelfDestruct ? time - npc.SelfDestructStart : 0f;
				
				#endregion

				#region Flight

				FlightMovementTarget = npc.Agent.Flight != null ? npc.Agent.Flight.MovementTarget : null;

				#endregion
			}
			
			public void Apply(object obj)
			{
				if (obj is not NPC npc)
					return;
				
				npc.SetAIState(
					AIMode, PreviousAIMode, 
					ActionMode, PreviousActionMode, 
					Destination, PreviousDestination, 
					AttackTargetObjectID, PreviousAttackTargetObjectID, 
					OtherTargetObjectID, PreviousOtherTargetObjectID, 
					AgentPosition,
					PatrolPath, PatrolStartAt, PatrolAlreadyWaited,
					UseWalkAfterwards,
					CarryDropAt,
					SwitchCastCooldown,
					ChaseInterruptTimer, ChaseInterruptDuration);
			
				npc.SetSelfDestructState(SelfDestructed, SelfDestructElapsed);

				var flight = npc.Agent.Flight;
				if (flight == null)
					return;
			
				flight.SetState(FlightMovementTarget);
			}
		}
	}
}