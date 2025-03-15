using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeRaycastsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;
		
		[ReadOnly]
		public NativeArray<SIndexWithCost> Neighbors;
		
		[WriteOnly]
		public NativeArray<SpherecastCommand> Commands;

		[ReadOnly]
		public float Radius;

		[ReadOnly]
		public QueryParameters Query;
		
		public void Execute(int index)
		{
			var neighbor = Neighbors[index];
			var neighborPos = Nodes[neighbor.Index].WorldPosition;
			
			var nodePos = Nodes[index / 26].WorldPosition;

			float3 direction;
			direction.x = neighborPos.x - nodePos.x;
			direction.y = neighborPos.y - nodePos.y;
			direction.z = neighborPos.z - nodePos.z;
			
			Commands[index] = new SpherecastCommand(nodePos, Radius, direction, Query, math.length(direction));
		}
	}
}