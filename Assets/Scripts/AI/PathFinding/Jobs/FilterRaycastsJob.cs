using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterRaycastsJob : IJobParallelFor
	{
		public NativeArray<SIndexWithCost> Neighbors;

		[ReadOnly]
		public NativeSlice<SRaycastHit> Hits;

		public void Execute(int index)
		{
			if (Hits[index].m_Collider == 0)
				return;

			var neighbor = Neighbors[index];
			neighbor.Connects = false;
			
			Neighbors[index] = neighbor;
		}
	}
}