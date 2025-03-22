using AI.PathFinding.Enums;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct GetObstructedJob : IJobFor
	{
		[ReadOnly]
		public NativeArray<ENodeAvailabilityFlags> Availabilities;

		[WriteOnly]
		public NativeList<int> Obstructed;

		public void Execute(int index)
		{
			var availability = Availabilities[index];
			if ((availability & ENodeAvailabilityFlags.Obstructed) == 0)
				return;

			Obstructed.Add(index);
		}
	}
}