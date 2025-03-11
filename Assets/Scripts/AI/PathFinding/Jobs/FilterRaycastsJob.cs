using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterRaycastsJob : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<SIndexWithCost> Neighbors;

		[ReadOnly]
		public NativeSlice<SRaycastHit> Hits;

		[ReadOnly]
		public SIndexWithCost Empty;

		public void Execute(int index)
		{
			if (Hits[index].m_Collider == 0)
				return;

			Neighbors[index] = Empty;
		}
	}
}