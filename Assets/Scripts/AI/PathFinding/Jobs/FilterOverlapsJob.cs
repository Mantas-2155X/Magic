using AI.PathFinding.Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterOverlapsJob : IJobParallelFor
	{
		public NativeArray<ENodeAvailabilityFlags> Availabilities;

		[ReadOnly]
		public NativeArray<ColliderHit> Hits;

		public void Execute(int index)
		{
			var availability = Availabilities[index];

			if (Hits[index].instanceID == 0)
				availability &= ~ENodeAvailabilityFlags.InsideObject;
			else
				availability |= ENodeAvailabilityFlags.InsideObject;

			Availabilities[index] = availability;
		}
	}
}