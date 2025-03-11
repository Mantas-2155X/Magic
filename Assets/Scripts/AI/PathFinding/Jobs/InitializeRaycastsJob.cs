using AI.PathFinding.Structs;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace AI.PathFinding.Jobs
{
	[BurstCompile]
	public struct InitializeRaycastsJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;
		
		[ReadOnly]
		public NativeArray<SIndexWithCost> Neighbors;
		
		[WriteOnly][NativeDisableParallelForRestriction]
		public NativeArray<RaycastCommand> Commands;

		public QueryParameters Query;
		
		public void Execute(int index)
		{
			var node = Nodes[index];
			var nodePos = node.WorldPosition;

			var startIndex = index * 26;

			for (var i = startIndex; i < startIndex + 26; i++)
			{
				var neighbor = Neighbors[i];
				var neighborPos = Nodes[neighbor.Index].WorldPosition;

				Vector3 nodePosVector;
				nodePosVector.x = nodePos.x;
				nodePosVector.y = nodePos.y;
				nodePosVector.z = nodePos.z;
				
				Vector3 directionVector;
				directionVector.x = neighborPos.x - nodePos.x;
				directionVector.y = neighborPos.y - nodePos.y;
				directionVector.z = neighborPos.z - nodePos.z;
			
				Commands[i] = new RaycastCommand(nodePosVector, directionVector, Query, directionVector.magnitude);
			}
		}
	}
}