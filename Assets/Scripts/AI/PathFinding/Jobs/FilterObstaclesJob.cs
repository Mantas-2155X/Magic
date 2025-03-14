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
		
		[WriteOnly]
		public NativeArray<bool> Obstructed;

		[ReadOnly]
		public float Radius;

		public void Execute(int index)
		{
			var node = Nodes[index];
			var nodePos = node.WorldPosition;

			for (var i = 0; i < Positions.Length; i++)
			{
				var position = Positions[i];
				var halfSize = HalfSizes[i];

				var minX = position.x - halfSize.x;
				var minY = position.y - halfSize.y;
				var minZ = position.z - halfSize.z;
				
				var maxX = position.x + halfSize.x;
				var maxY = position.y + halfSize.y;
				var maxZ = position.z + halfSize.z;

				if ((nodePos.x >= minX && nodePos.x <= maxX) && (nodePos.y >= minY && nodePos.y <= maxY) && (nodePos.z >= minZ && nodePos.z <= maxZ))
				{
					Obstructed[index] = true;
					break;
				}
			}
		}
	}
}