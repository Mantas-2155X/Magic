using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

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
		public NativeArray<Matrix4x4> InverseMatrices;

		[ReadOnly]
		public NativeArray<float> AreaCosts;
		
		[ReadOnly]
		public float HalfRadius;

		public void Execute(int index)
		{
			var node = Nodes[index];
			var nodePos = node.WorldPosition;
			var positionsLength = Positions.Length;

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
					node.AreaCost = AreaCosts[i];
					Nodes[index] = node;
					break;
				}
			}
		}
	}
}