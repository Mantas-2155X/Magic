using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct FilterObstaclesJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;

		[ReadOnly]
		public NativeArray<float3> Positions;
		
		[ReadOnly]
		public NativeArray<float3> HalfSizes;
		
		public NativeArray<ENodeAvailabilityFlags> Availabilities;

		[ReadOnly]
		public float HalfRadius;

		public void Execute(int index)
		{
			var node = Nodes[index];
			var nodePos = node.WorldPosition;

			var availability = Availabilities[index];
			availability &= ~ENodeAvailabilityFlags.Obstructed;
			
			for (var i = 0; i < Positions.Length; i++)
			{
				var position = Positions[i];
				var halfSize = HalfSizes[i];

				var direction = math.normalize(position - nodePos);
				var closestPoint = direction * HalfRadius + nodePos;
				
				var minX = position.x - halfSize.x;
				var minY = position.y - halfSize.y;
				var minZ = position.z - halfSize.z;
				
				var maxX = position.x + halfSize.x;
				var maxY = position.y + halfSize.y;
				var maxZ = position.z + halfSize.z;

				if ((closestPoint.x >= minX && closestPoint.x <= maxX) && (closestPoint.y >= minY && closestPoint.y <= maxY) && (closestPoint.z >= minZ && closestPoint.z <= maxZ))
				{
					availability |= ENodeAvailabilityFlags.Obstructed;
					break;
				}
			}
			
			Availabilities[index] = availability;
		}
	}
}