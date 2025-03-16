using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterRaycastsJob : IJobParallelFor
	{
		public NativeArray<SIndexWithCost> Neighbors;

		[ReadOnly]
		public NativeSlice<RaycastHit> Hits;

		public void Execute(int index)
		{
			if (Hits[index].colliderInstanceID == 0)
				return;

			var neighbor = Neighbors[index];
			neighbor.Connects = false;
			
			Neighbors[index] = neighbor;
		}
	}
}