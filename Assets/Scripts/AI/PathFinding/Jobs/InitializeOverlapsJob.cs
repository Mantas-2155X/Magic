using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeOverlapsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;
		
		[WriteOnly]
		public NativeArray<OverlapSphereCommand> Commands;
		
		[ReadOnly]
		public float Radius;

		[ReadOnly]
		public QueryParameters Query;
		
		public void Execute(int index)
		{
			Commands[index] = new OverlapSphereCommand(Nodes[index].WorldPosition, Radius, Query);
		}
	}
}