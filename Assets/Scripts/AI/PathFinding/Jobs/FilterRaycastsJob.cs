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
		[WriteOnly]
		public NativeArray<SIndexWithCost> Neighbors;

		[ReadOnly]
		public NativeArray<RaycastHit> Hits;

		public void Execute(int index)
		{
			if (Hits[index].colliderInstanceID == 0)
				return;

			Neighbors[index] = new SIndexWithCost();
		}
	}
}