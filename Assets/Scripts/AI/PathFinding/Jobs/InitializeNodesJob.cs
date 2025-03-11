using AI.PathFinding.Enums;
using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeNodesJob : IJob
	{
		[WriteOnly]
		public NativeArray<SNode> Nodes;

		[ReadOnly]
		public float3 Position;

		[ReadOnly]
		public float Distance;
		
		[ReadOnly]
		public int XSize;
		
		[ReadOnly]
		public int YSize;
		
		[ReadOnly]
		public int ZSize;

		public void Execute()
		{
			var index = 0;
			
			for (var x = 0; x < XSize; x++)
			{
				for (var y = 0; y < YSize; y++)
				{
					for (var z = 0; z < ZSize; z++)
					{
						int3 gridPosition;
						gridPosition.x = x;
						gridPosition.y = y;
						gridPosition.z = z;

						float3 worldPosition;
						worldPosition.x = x * Distance + Position.x;
						worldPosition.y = y * Distance + Position.y;
						worldPosition.z = z * Distance + Position.z;
					
						Nodes[index] = new SNode
						{
							Index = index,
							GridPosition = gridPosition,
							WorldPosition = worldPosition,
							Availability = ENodeAvailabilityFlags.Available,
							GCost = float.MaxValue,
							HCost = 0f,
							FCost = 0f,
							Connection = -1
						};
					
						index++;
					}
				}
			}
		}
	}
}