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
			
			var availability = node.Availability;
			availability &= ~ENodeAvailabilityFlags.Available;
			availability |= ENodeAvailabilityFlags.InsideObject;

			node.Availability = availability;
			Nodes[index] = node;
		}
	}
}