using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeAreasJob : IJobParallelFor
	{
		public NativeArray<SNode> Nodes;

		[ReadOnly]
		public NativeArray<float3> Positions;
		
		[ReadOnly]
		public NativeArray<float3> HalfSizes;

		[ReadOnly]
		public NativeArray<float> AreaCosts;
		
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

				var direction = math.normalize(position - nodePos);
				var closestPoint = direction * Radius + nodePos;
				
				var minX = position.x - halfSize.x;
				var minY = position.y - halfSize.y;
				var minZ = position.z - halfSize.z;
				
				var maxX = position.x + halfSize.x;
				var maxY = position.y + halfSize.y;
				var maxZ = position.z + halfSize.z;

				if ((closestPoint.x >= minX && closestPoint.x <= maxX) && (closestPoint.y >= minY && closestPoint.y <= maxY) && (closestPoint.z >= minZ && closestPoint.z <= maxZ))
				{
					node.AreaCost = AreaCosts[i];
					Nodes[index] = node;
					break;
				}
			}
		}
	}
}