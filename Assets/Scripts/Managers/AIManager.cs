using System.Collections.Generic;
using AI;
using AI.Base;
using AI.Interfaces;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

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
		public float AutoTargetEvery = 0.1f;
		
		private readonly List<Transform> transforms = new ();
		private readonly List<IAlive> alives = new ();

		private TransformAccessArray transformsNative;
		
		private NativeArray<float3> positionsNative;
		private NativeArray<int> decisionsNative;
		
		private JobHandle autoTargetPositionsHandle;
		private JobHandle autoTargetDecisionsHandle;

		private bool autoTargetComplete;
		private bool nativeDataDirty;
		private float lastAutoTarget;
		
		public void Awake()
		{
			Instance = this;
			
			transformsNative = new TransformAccessArray();
			
			positionsNative = new NativeArray<float3>(0, Allocator.Persistent);
			decisionsNative = new NativeArray<int>(0, Allocator.Persistent);
			
			BaseAlive.OnDeathEvent.AddListener(onDeath);
		}

		public void OnDestroy()
		{
			destroyNativeData();
		}

		public void Update()
		{
			var time = Time.time;
			
			if (lastAutoTarget + AutoTargetEvery > time)
				return;

			lastAutoTarget = time;

			if (nativeDataDirty)
			{
				destroyNativeData();
			
				transforms.Clear();
				alives.Clear();
			
				transforms.Add(Player.transform);
				alives.Add(Player);
			
				for (var i = 0; i < NPCs.Count; i++)
				{
					var npc = NPCs[i];
					if (!npc.IsAlive)
						continue;
				
					transforms.Add(npc.transform);
					alives.Add(npc);
				}

				transformsNative = new TransformAccessArray(transforms.ToArray());
			
				positionsNative = new NativeArray<float3>(transforms.Count, Allocator.Persistent);
				decisionsNative = new NativeArray<int>(transforms.Count, Allocator.Persistent);

				nativeDataDirty = false;
			}

			var positionsJob = new AutoTargetPositionsJob { Positions = positionsNative };
			autoTargetPositionsHandle = positionsJob.Schedule(transformsNative);

			var decisionsJob = new AutoTargetDecisionsJob { Positions = positionsNative, Decisions = decisionsNative };
			autoTargetDecisionsHandle = decisionsJob.Schedule(transformsNative, autoTargetPositionsHandle);

			autoTargetComplete = true;
		}

		public void LateUpdate()
		{
			if (!autoTargetComplete)
				return;
			
			autoTargetPositionsHandle.Complete();
			autoTargetDecisionsHandle.Complete();

			if (decisionsNative.IsCreated)
			{
				for (var i = 0; i < decisionsNative.Length; i++)
				{
					var decision = decisionsNative[i];
					if (decision == int.MaxValue)
						continue;

					var thisAlive = alives[i];
					if (thisAlive == null || !thisAlive.IsAlive || thisAlive is not NPC npc)
						continue;
				
					var otherAlive = alives[decision];
					if (otherAlive == null || !otherAlive.IsAlive)
						continue;
				
					npc.AssignTarget((Component)otherAlive);
				}
			}
			
			autoTargetComplete = false;
		}

		private void destroyNativeData()
		{
			if (transformsNative.isCreated)
				transformsNative.Dispose();

			if (positionsNative.IsCreated)
				positionsNative.Dispose();
			
			if (decisionsNative.IsCreated)
				decisionsNative.Dispose();
		}
		
		private void onDeath(IAlive alive, object source)
		{
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
			
			[WriteOnly]
			public NativeArray<int> Decisions;
		
			public void Execute(int index, TransformAccess transform)
			{
				var thisPosition = transform.position;

				var smallestDistance = float.PositiveInfinity;
				var smallestIndex = int.MaxValue;
				
				for (var i = 0; i < Positions.Length; i++)
				{
					if (index == i)
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
			ObjectManager.Instance.CreatePortal(position);
			
			var go = Instantiate(Resources.Load<GameObject>("Alives/NPC"));
			go.name = $"NPC {NPCs.Count}";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;
			
			go.SetActive(true);
			
			var npc = go.GetComponent<NPC>();
			npc.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			NPCs.Add(npc);
			nativeDataDirty = true;
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
			
			var go = Instantiate(Resources.Load<GameObject>("Alives/Player"));
			go.name = "Player";
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Characters);
			tr.position = position;
			tr.eulerAngles = angles;

			go.SetActive(true);
			
			var player = go.GetComponent<Player>();
			player.Spawn(startingHealth, overloadHealth, regenerateHealth, startingMana, overloadMana, regenerateMana, speed);

			Player = player;
			nativeDataDirty = true;
			return player;
		}
	}
}