using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterOverlapsJob : IJobParallelFor
	{
		public NativeArray<SNode> Nodes;

		[ReadOnly]
		public NativeArray<ColliderHit> Hits;

		public void Execute(int index)
		{
			if (Hits[index].instanceID == 0)
				return;

			var node = Nodes[index];
			node.Availability = ENodeAvailability.InsideObject;
			Nodes[index] = node;
		}
	}
}