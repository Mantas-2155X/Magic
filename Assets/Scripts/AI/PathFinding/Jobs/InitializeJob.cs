using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeJob : IJob
	{
		[WriteOnly]
		public NativeArray<SNode> Nodes;
		
		[WriteOnly]
		public NativeArray<SIndexWithCost> Neighbors;

		[WriteOnly]
		public NativeArray<OverlapSphereCommand> OverlapCommands;
		
		[WriteOnly]
		public NativeArray<ColliderHit> OverlapResults;
		
		[WriteOnly]
		public NativeArray<RaycastCommand> RaycastCommands;
		
		[WriteOnly]
		public NativeArray<RaycastHit> RaycastResults;
		
		public unsafe void Execute()
		{
			UnsafeUtility.MemClear(Nodes.GetUnsafePtr(), (long)Nodes.Length * sizeof(SNode));
			UnsafeUtility.MemClear(Neighbors.GetUnsafePtr(), (long)Neighbors.Length * sizeof(SIndexWithCost));
			
			UnsafeUtility.MemClear(OverlapCommands.GetUnsafePtr(), (long)OverlapCommands.Length * sizeof(OverlapSphereCommand));
			UnsafeUtility.MemClear(OverlapResults.GetUnsafePtr(), (long)OverlapResults.Length * sizeof(ColliderHit));
			
			UnsafeUtility.MemClear(RaycastCommands.GetUnsafePtr(), (long)RaycastCommands.Length * sizeof(RaycastCommand));
			UnsafeUtility.MemClear(RaycastResults.GetUnsafePtr(), (long)RaycastResults.Length * sizeof(RaycastHit));
		}
	}
}