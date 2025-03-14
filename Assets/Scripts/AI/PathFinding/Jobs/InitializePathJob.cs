using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializePathJob : IJobParallelFor
	{
		[WriteOnly]
		public NativeArray<float> GCosts;
		
		[WriteOnly]
		public NativeArray<float> HCosts;
		
		[WriteOnly]
		public NativeArray<float> FCosts;
		
		[WriteOnly]
		public NativeArray<int> Connections;
		
		public void Execute(int index)
		{
			GCosts[index] = float.MaxValue;
			HCosts[index] = 0;
			FCosts[index] = 0;
			Connections[index] = -1;
		}
	}
}