using System.Collections.Generic;
using AI;
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
		
		private readonly List<Transform> aliveTransforms = new ();
		private readonly List<NPC> aliveNPCs = new ();

		private TransformAccessArray aliveTransformsNative;
		
		private NativeArray<float3> alivePositionsNative;
		private NativeArray<int> aliveDecisionsNative;
		
		private JobHandle autoTargetPositionsHandle;
		private JobHandle autoTargetDecisionsHandle;
		
		public void Awake()
		{
			Instance = this;
			
			aliveTransformsNative = new TransformAccessArray();
			
			alivePositionsNative = new NativeArray<float3>(0, Allocator.Persistent);
			aliveDecisionsNative = new NativeArray<int>(0, Allocator.Persistent);
		}

		public void OnDestroy()
		{
			if (aliveTransformsNative.isCreated)
				aliveTransformsNative.Dispose();
			
			if (alivePositionsNative.IsCreated)
				alivePositionsNative.Dispose();
			
			if (aliveDecisionsNative.IsCreated)
				aliveDecisionsNative.Dispose();
		}

		public void Update()
		{
			if (aliveTransformsNative.isCreated)
				aliveTransformsNative.Dispose();

			if (alivePositionsNative.IsCreated)
				alivePositionsNative.Dispose();
			
			if (aliveDecisionsNative.IsCreated)
				aliveDecisionsNative.Dispose();
			
			aliveTransforms.Clear();
			aliveNPCs.Clear();
			
			for (var i = 0; i < NPCs.Count; i++)
			{
				var npc = NPCs[i];
				if (!npc.IsAlive)
					continue;
				
				aliveTransforms.Add(npc.transform);
				aliveNPCs.Add(npc);
			}

			aliveTransformsNative = new TransformAccessArray(aliveTransforms.ToArray());
			
			alivePositionsNative = new NativeArray<float3>(aliveTransforms.Count, Allocator.Persistent);
			aliveDecisionsNative = new NativeArray<int>(aliveTransforms.Count, Allocator.Persistent);

			var positionsJob = new AutoTargetPositionsJob { Positions = alivePositionsNative };
			autoTargetPositionsHandle = positionsJob.Schedule(aliveTransformsNative);

			var decisionsJob = new AutoTargetDecisionsJob { Positions = alivePositionsNative, Decisions = aliveDecisionsNative };
			autoTargetDecisionsHandle = decisionsJob.Schedule(aliveTransformsNative, autoTargetPositionsHandle);
		}

		public void LateUpdate()
		{
			autoTargetPositionsHandle.Complete();
			autoTargetDecisionsHandle.Complete();
			
			for (var i = 0; i < aliveDecisionsNative.Length; i++)
			{
				var decision = aliveDecisionsNative[i];
				if (decision == int.MaxValue)
					continue;

				var thisNPC = aliveNPCs[i];
				if (!thisNPC.IsAlive)
					continue;
				
				var otherNPC = aliveNPCs[decision];
				if (!otherNPC.IsAlive)
					continue;
				
				thisNPC.AssignTarget(otherNPC);
			}
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
					
					var distance = math.distance(thisPosition, otherPosition);
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
			return player;
		}
	}
}