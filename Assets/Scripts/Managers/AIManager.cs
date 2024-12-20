using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Enums;
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

		// Body Collider -> Alive
		public readonly Dictionary<Collider, IAlive> AlivesColliderMap = new ();
		
		public static bool NativeDataDirty;

		private readonly List<Transform> transforms = new ();
		private readonly List<IAlive> alives = new ();
		private readonly List<EAIType> aiTypes = new ();
		private readonly List<EAIType> aiTargets = new ();

		private TransformAccessArray transformsNative;
		
		private NativeArray<float3> positionsNative;
		private NativeArray<int> decisionsNative;
		private NativeArray<EAIType> aiTypesNative;
		private NativeArray<EAIType> aiTargetsNative;
		
		private JobHandle autoTargetPositionsHandle;
		private JobHandle autoTargetDecisionsHandle;
		
		public void Awake()
		{
			Instance = this;
			
			transformsNative = new TransformAccessArray();
			
			positionsNative = new NativeArray<float3>(0, Allocator.Persistent);
			decisionsNative = new NativeArray<int>(0, Allocator.Persistent);
			aiTypesNative = new NativeArray<EAIType>(0, Allocator.Persistent);
			aiTargetsNative = new NativeArray<EAIType>(0, Allocator.Persistent);
			
			BaseAlive.OnDeathEvent.AddListener(onDeath);
			BaseAlive.OnDamageEvent.AddListener(onDamage);
		}

		public void OnDestroy()
		{
			autoTargetDecisionsHandle.Complete();
			destroyNativeData(); 
		}

		public void Update()
		{
			if (autoTargetDecisionsHandle.IsCompleted)
			{
				autoTargetDecisionsHandle.Complete();

				if (decisionsNative.IsCreated && !NativeDataDirty)
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
			
			if (NativeDataDirty)
			{
				destroyNativeData();
			
				transforms.Clear();
				alives.Clear();
				aiTypes.Clear();
				aiTargets.Clear();
			
				transforms.Add(Player.GetTransform());
				alives.Add(Player);
				aiTypes.Add(EAIType.Player);
				aiTargets.Add(EAIType.None);
				
				for (var i = 0; i < NPCs.Count; i++)
				{
					var npc = NPCs[i];
					if (!npc.IsAlive)
						continue;
				
					transforms.Add(npc.GetTransform());
					alives.Add(npc);
					aiTypes.Add(npc.AIType);
					aiTargets.Add(npc.AutoTarget);
				}

				transformsNative = new TransformAccessArray(transforms.ToArray());
			
				positionsNative = new NativeArray<float3>(transforms.Count, Allocator.Persistent);
				decisionsNative = new NativeArray<int>(transforms.Count, Allocator.Persistent);
				aiTypesNative = new NativeArray<EAIType>(aiTypes.ToArray(), Allocator.Persistent);
				aiTargetsNative = new NativeArray<EAIType>(aiTargets.ToArray(), Allocator.Persistent);

				NativeDataDirty = false;
			}

			var positionsJob = new AutoTargetPositionsJob { Positions = positionsNative };
			autoTargetPositionsHandle = positionsJob.Schedule(transformsNative);

			var decisionsJob = new AutoTargetDecisionsJob { Positions = positionsNative, Decisions = decisionsNative, Types = aiTypesNative, Targets = aiTargetsNative};
			autoTargetDecisionsHandle = decisionsJob.Schedule(transformsNative, autoTargetPositionsHandle);
		}

		private void destroyNativeData()
		{
			if (transformsNative.isCreated)
				transformsNative.Dispose();

			if (positionsNative.IsCreated)
				positionsNative.Dispose();
			
			if (decisionsNative.IsCreated)
				decisionsNative.Dispose();
			
			if (aiTypesNative.IsCreated)
				aiTypesNative.Dispose();
			
			if (aiTargetsNative.IsCreated)
				aiTargetsNative.Dispose();
		}
		
		private void onDeath(IAlive alive, object source)
		{
			for (var i = 0; i < NPCs.Count; i++)
			{
				var npc = NPCs[i];
				if (npc.Target != (Component)alive)
					continue;

				npc.AssignTarget(null);
			}
			
			NativeDataDirty = true;
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
			
			if (aggressor == null || aggressor == alive || !aggressor.IsAlive)
				return;
			
			npc.AssignTarget((Component)aggressor);
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
			public NativeArray<EAIType> Types;
			
			[ReadOnly]
			public NativeArray<EAIType> Targets;
			
			[WriteOnly]
			public NativeArray<int> Decisions;
		
			public void Execute(int index, TransformAccess transform)
			{
				var thisTarget = Targets[index];
				
				// Auto target is not set - skip entirely
				if (thisTarget == EAIType.None)
				{
					Decisions[index] = int.MaxValue;
					return;
				}
				
				// Auto target is set to only the player - set to the first index (player)
				if (thisTarget == EAIType.Player)
				{
					Decisions[index] = 0;
					return;
				}

				var smallestIndex = int.MaxValue;
				var smallestDistance = float.PositiveInfinity;
				
				var thisPosition = transform.position;
				
				for (var i = 0; i < Positions.Length; i++)
				{
					if (index == i)
						continue;
					
					var otherType = Types[i];
					
					// Auto target does not include this type - skip
					if ((otherType & thisTarget) == 0)
						continue;
					
					var otherPosition = Positions[i];
					
					var distance = math.distancesq(thisPosition, otherPosition);
					if (distance < smallestDistance)
					{
						smallestDistance = distance;
						smallestIndex = i;
					}
				}

				Decisions[index] = smallestIndex;
			}
		}
		
		public NPC CreateNPC(Vector3 position, Vector3 angles, float startingHealth = 50, float overloadHealth = 76, float regenerateHealth = 0.5f, float startingMana = 250, float overloadMana = 376, float regenerateMana = 7, float speed = 7f)
		{
			ObjectManager.Instance.CreateObject(ObjectManager.Instance.GetObject("Portal"), position, Vector3.zero);
			
			var go = Instantiate(Resources.Load<GameObject>("NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			npc.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			AlivesColliderMap[npc.Body.BodyCollider] = npc;
			
			NPCs.Add(npc);
			NativeDataDirty = true;
			return npc;
		}
		public Player CreatePlayer(Vector3 position, Vector3 angles, float startingHealth = 100, float overloadHealth = 151, float regenerateHealth = 0.5f, float startingMana = 100, float overloadMana = 151, float regenerateMana = 5, float speed = 7f)
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
			player.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			AlivesColliderMap[player.Body.BodyCollider] = player;

			Player = player;
			NativeDataDirty = true;
			return player;
		}
	}
}