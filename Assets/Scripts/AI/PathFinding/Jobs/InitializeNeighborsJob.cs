using System.Runtime.CompilerServices;
using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeNeighborsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;

		[WriteOnly][NativeDisableParallelForRestriction]
		public NativeArray<SIndexWithCost> Neighbors;
		
		[ReadOnly]
		public float Accuracy;

		[ReadOnly]
		public int XSize;
		
		[ReadOnly]
		public int YSize;
		
		[ReadOnly]
		public int ZSize;
		
		public void Execute(int index)
		{
			var node = Nodes[index];
			
			var gridPos = node.GridPosition;
			var worldPos = node.WorldPosition;
			
			var x = gridPos.x;
			var y = gridPos.y;
			var z = gridPos.z;

			var addIndex = index * 26;
			
			for (var cX = -1; cX < 2; cX++)
			{
				for (var cY = -1; cY < 2; cY++)
				{
					for (var cZ = -1; cZ < 2; cZ++)
					{
						var neighborX = x + cX;
						if (neighborX < 0 || neighborX >= XSize)
							continue;
					
						var neighborY = y + cY;
						if (neighborY < 0 || neighborY >= YSize)
							continue;

						var neighborZ = z + cZ;
						if (neighborZ < 0 || neighborZ >= ZSize)
							continue;

						var neighborIndex = getNodeIndex(neighborX, neighborY, neighborZ);
						if (neighborIndex == index)
							continue;

						var cost = math.distance(gridPos, Nodes[neighborIndex].GridPosition);

						SIndexWithCost neighbor;
						neighbor.Index = neighborIndex;
						neighbor.Cost = cost * Accuracy;
						neighbor.Connects = true;
						
						Neighbors[addIndex] = neighbor;
						addIndex++;
					}
				}
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int getNodeIndex(int x, int y, int z)
		{
			return x * (ZSize * YSize) + y * ZSize + z;
		}
	}
}