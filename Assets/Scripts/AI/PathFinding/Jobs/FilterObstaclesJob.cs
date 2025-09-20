using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
		
		[ReadOnly]
		public NativeArray<Matrix4x4> InverseMatrices;
		
		public NativeArray<ENodeAvailabilityFlags> Availabilities;

		[ReadOnly]
		public float HalfRadius;

		public void Execute(int index)
		{
			var node = Nodes[index];
			var nodePos = node.WorldPosition;
			var positionsLength = Positions.Length;

			var availability = Availabilities[index];
			availability &= ~ENodeAvailabilityFlags.Obstructed;
			
			for (var i = 0; i < positionsLength; i++)
			{
				var position = Positions[i];
				var halfSize = HalfSizes[i];
				var inverseMatrix = InverseMatrices[i];

				var direction = math.normalize(position - nodePos);
				var closestPoint = direction * HalfRadius + nodePos;
				
				var transformedPoint = inverseMatrix.MultiplyPoint(closestPoint);
				
				if ((transformedPoint.x >= -halfSize.x && transformedPoint.x <= halfSize.x) && 
				    (transformedPoint.y >= -halfSize.y && transformedPoint.y <= halfSize.y) && 
				    (transformedPoint.z >= -halfSize.z && transformedPoint.z <= halfSize.z))
				{
					availability |= ENodeAvailabilityFlags.Obstructed;
					break;
				}
			}
			
			Availabilities[index] = availability;
		}
	}
}