using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Interfaces;
using Attacks.Interfaces;
using Projectiles.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;
using Weapons.Interfaces;

namespace Managers
{
	public class AIManager : MonoBehaviour
	{
		public static AIManager Instance;
		
		[SerializeField]
		public Player Player;

		[SerializeField]
		public List<NPC> NPCs = new ();

		[SerializeField]
		public bool AutoTarget = true;
		
		// Body Collider -> Alive
		public readonly Dictionary<Collider, IAlive> AlivesColliderMap = new ();
		
		private static bool nativeDataDirty;

		private readonly List<Transform> transforms = new ();
		private readonly List<IAlive> alives = new ();
		private readonly List<int> relationshipGroups = new ();

		private TransformAccessArray transformsNative;
		
		private NativeArray<float3> positionsNative;
		private NativeArray<int> decisionsNative;
		private NativeArray<int> relationshipGroupsNative;
		
		private JobHandle autoTargetPositionsHandle;
		private JobHandle autoTargetDecisionsHandle;

		private bool updateTargets;
		
		public void Awake()
		{
			Instance = this;
			
			transformsNative = new TransformAccessArray();
			
			positionsNative = new NativeArray<float3>(0, Allocator.Persistent);
			decisionsNative = new NativeArray<int>(0, Allocator.Persistent);
			relationshipGroupsNative = new NativeArray<int>(0, Allocator.Persistent);
			
			BaseAlive.OnDamageEvent.AddListener(onDamage);
			BaseAlive.OnSpawnEvent.AddListener(onSpawn);
			BaseAlive.OnDeathEvent.AddListener(onDeath);
			BaseAlive.OnRelationshipGroupChangedEvent.AddListener(onRelationshipGroupChanged);
		}

		public void Update()
		{
			handleTargets();

			if (AutoTarget)
			{
				assignAutoTargetResults();
				prepareAutoTargetData();
				startAutoTargetJobs();
			}
		}

		public void OnDestroy()
		{
			autoTargetDecisionsHandle.Complete();
			destroyNativeData(); 
		}

		private void destroyNativeData()
		{
			if (transformsNative.isCreated)
				transformsNative.Dispose();

			if (positionsNative.IsCreated)
				positionsNative.Dispose();
			
			if (decisionsNative.IsCreated)
				decisionsNative.Dispose();
			
			if (relationshipGroupsNative.IsCreated)
				relationshipGroupsNative.Dispose();
		}

		private void handleTargets()
		{
			if (!updateTargets)
				return;

			updateTargets = false;
			
			for (var i = 0; i < NPCs.Count; i++)
			{
				var npc = NPCs[i];
				if (!npc.IsAlive)
					continue;

				if (npc.Target is not IAlive target)
					continue;

				if (!target.IsAlive)
				{
					npc.AssignTarget(null);
					nativeDataDirty = true;
				}
				else if (npc.RelationshipGroup == target.RelationshipGroup)
				{
					npc.AssignTarget(null);
					nativeDataDirty = true;
				}
			}
		}

		private void assignAutoTargetResults()
		{
			if (!autoTargetDecisionsHandle.IsCompleted)
				return;

			autoTargetDecisionsHandle.Complete();

			if (decisionsNative.IsCreated && !nativeDataDirty)
			{
				for (var i = 0; i < decisionsNative.Length; i++)
				{
					var decision = decisionsNative[i];
					if (decision == int.MaxValue)
						continue;

					var thisAlive = alives[i];
					if (thisAlive == null || !thisAlive.IsAlive)
						continue;

					var otherAlive = alives[decision];
					if (otherAlive == null || !otherAlive.IsAlive)
						continue;
					
					// only npcs have auto target
					if (thisAlive is not NPC npc)
						continue;
					
					// don't interrupt casting 
					if (npc.Weapon != null && npc.Weapon.IsCasting)
						continue;

					npc.AssignTarget((Component)otherAlive);
				}
			}
		}

		private void prepareAutoTargetData()
		{
			if (nativeDataDirty)
			{
				destroyNativeData();
			
				transforms.Clear();
				alives.Clear();
				relationshipGroups.Clear();
			
				transforms.Add(Player.GetTransform());
				alives.Add(Player);
				relationshipGroups.Add(Player.RelationshipGroup);
				
				for (var i = 0; i < NPCs.Count; i++)
				{
					var npc = NPCs[i];
					if (!npc.IsAlive)
						continue;
				
					transforms.Add(npc.GetTransform());
					alives.Add(npc);
					relationshipGroups.Add(npc.RelationshipGroup);
				}

				transformsNative = new TransformAccessArray(transforms.ToArray());
			
				positionsNative = new NativeArray<float3>(transforms.Count, Allocator.Persistent);
				decisionsNative = new NativeArray<int>(transforms.Count, Allocator.Persistent);
				relationshipGroupsNative = new NativeArray<int>(relationshipGroups.ToArray(), Allocator.Persistent);

				nativeDataDirty = false;
			}
		}

		private void startAutoTargetJobs()
		{
			var positionsJob = new AutoTargetPositionsJob { Positions = positionsNative };
			autoTargetPositionsHandle = positionsJob.Schedule(transformsNative);

			var decisionsJob = new AutoTargetDecisionsJob { Positions = positionsNative, Decisions = decisionsNative, RelationshipGroups = relationshipGroupsNative};
			autoTargetDecisionsHandle = decisionsJob.Schedule(transformsNative, autoTargetPositionsHandle);
		}
		
		private void onDamage(IAlive alive, float damage, object source)
		{
			if (!alive.IsAlive || alive is not NPC npc)
				return;

			IAlive aggressor = null;
			
			switch (source)
			{
				case IAlive aggr:
					aggressor = aggr;
					break;
				case IWeapon weapon:
					aggressor = weapon.GetAlive();
					break;
				case IAttack attack:
					aggressor = attack.GetAlive();
					break;
				case IProjectile projectile:
					aggressor = projectile.GetAlive();
					break;
			}
			
			if (aggressor == null || !aggressor.IsAlive || aggressor == alive || aggressor.RelationshipGroup == alive.RelationshipGroup)
				return;
			
			npc.AssignTarget((Component)aggressor);
		}

		private void onSpawn(IAlive alive)
		{
			updateTargets = true;
			nativeDataDirty = true;
		}
		
		private void onDeath(IAlive alive, object source)
		{
			updateTargets = true;
			nativeDataDirty = true;
		}

		private void onRelationshipGroupChanged(IAlive alive, int previousRelationshipGroup, int newRelationshipGroup)
		{
			updateTargets = true;
			nativeDataDirty = true;
		}

		[BurstCompile]
		public struct AutoTargetPositionsJob : IJobParallelForTransform
		{
			[WriteOnly]
			public NativeArray<float3> Positions;
		
			public void Execute(int index, TransformAccess transform)
			{
				Positions[index] = transform.position;
			}
		}
		
		[BurstCompile]
		public struct AutoTargetDecisionsJob : IJobParallelForTransform
		{
			[ReadOnly]
			public NativeArray<float3> Positions;

			[ReadOnly]
			public NativeArray<int> RelationshipGroups;
			
			[WriteOnly]
			public NativeArray<int> Decisions;
		
			public void Execute(int index, TransformAccess transform)
			{
				var thisRelationshipGroup = RelationshipGroups[index];

				var smallestIndex = int.MaxValue;
				var smallestDistance = float.PositiveInfinity;
				
				var thisPosition = transform.position;
				
				for (var i = 0; i < Positions.Length; i++)
				{
					if (index == i)
						continue;
					
					// Don't target the same group
					if (thisRelationshipGroup == RelationshipGroups[i])
						continue;
					
					var distance = math.distancesq(thisPosition, Positions[i]);
					if (distance >= smallestDistance)
						continue;
					
					smallestDistance = distance;
					smallestIndex = i;
				}

				Decisions[index] = smallestIndex;
			}
		}
		
		public NPC CreateNPC(Vector3 position, Vector3 angles, float startingHealth = 50, float overloadHealth = 76, float regenerateHealth = 0.5f, float startingMana = 250, float overloadMana = 376, float regenerateMana = 7, float speed = 7f, int relationshipGroup = 0)
		{
			ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("Portal"), position, Vector3.zero);
			
			var go = Instantiate(Resources.Load<GameObject>("NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			npc.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed, relationshipGroup);

			AlivesColliderMap[npc.Body.BodyCollider] = npc;
			
			NPCs.Add(npc);
			nativeDataDirty = true;
			return npc;
		}
		public Player CreatePlayer(Vector3 position, Vector3 angles, float startingHealth = 100, float overloadHealth = 151, float regenerateHealth = 0.5f, float startingMana = 100, float overloadMana = 151, float regenerateMana = 5, float speed = 7f, int relationshipGroup = -1)
		{
			if (Player != null)
			{
				if (Player.IsAlive)
					Player.Kill(null);
				
				Player = null;
			}
			
			var go = Instantiate(Resources.Load<GameObject>("Player"));
			go.name = "Player";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;

			go.SetActive(true);
			
			var player = go.GetComponent<Player>();
			player.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed, relationshipGroup);

			AlivesColliderMap[player.Body.BodyCollider] = player;

			Player = player;
			nativeDataDirty = true;
			return player;
		}
	}
}