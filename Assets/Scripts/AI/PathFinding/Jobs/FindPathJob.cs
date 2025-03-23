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
	public struct FindPathJob : IJob
	{
		[ReadOnly]
		public NativeArray<SNode> Nodes;
		
		[ReadOnly]
		public NativeArray<SIndexWithCost> Neighbors;

		[ReadOnly]
		public NativeArray<ENodeAvailabilityFlags> Availabilities;
		
		public NativeArray<float> GCosts;
		public NativeArray<float> HCosts;
		public NativeArray<float> FCosts;
		public NativeArray<int> Connections;
		
		[WriteOnly]
		public NativeList<int> ResultingPath;

		public NativeHashSet<int> SearchedNodes;
		public NativeList<int> ToSearchNodes;
		
		[ReadOnly]
		public float3 StartPosition;
		
		[ReadOnly]
		public float3 EndPosition;
		
		public void Execute()
		{
			var startNodeIndex = findClosestNode(StartPosition);
			var endNodeIndex = findClosestNode(EndPosition);

			var startNode = Nodes[startNodeIndex];
			var endNode = Nodes[endNodeIndex];

			var endGridPosition = endNode.GridPosition;
			
			var distanceBetweenPoints = math.distance(startNode.GridPosition, endGridPosition);

			GCosts[startNodeIndex] = 0f;
			HCosts[startNodeIndex] = distanceBetweenPoints;
			FCosts[startNodeIndex] = distanceBetweenPoints;
			
			ToSearchNodes.Add(startNodeIndex);
			
			while (ToSearchNodes.Length > 0)
			{
				var nodeIndex = ToSearchNodes[0];
				
				var nodeHCost = HCosts[nodeIndex];
				var nodeFCost = FCosts[nodeIndex];
				
				for (var i = 0; i < ToSearchNodes.Length; i++)
				{
					var searchingNodeIndex = ToSearchNodes[i];
					
					var searchingHCost = HCosts[searchingNodeIndex];
					var searchingFCost = FCosts[searchingNodeIndex];
					
					if (searchingFCost < nodeFCost || searchingFCost == nodeFCost && searchingHCost < nodeHCost)
						nodeIndex = searchingNodeIndex;
				}

				ToSearchNodes.RemoveAt(ToSearchNodes.IndexOf(nodeIndex));
				SearchedNodes.Add(nodeIndex);

				if (nodeIndex == endNodeIndex)
				{
					while (endNodeIndex != startNodeIndex)
					{
						ResultingPath.Add(endNodeIndex);
						endNodeIndex = Connections[endNodeIndex];
					}
		
					ResultingPath.Add(startNodeIndex);
					return;
				}
			
				calculateNeighbors(nodeIndex, endGridPosition);
			}
		}

		private int findClosestNode(float3 worldPosition)
		{
			var closestDistance = Mathf.Infinity;
			var closestNode = -1;

			for (var i = 0; i < Nodes.Length; i++)
			{
				if (Availabilities[i] != ENodeAvailabilityFlags.Available)
					continue;

				var dist = math.distancesq(Nodes[i].WorldPosition, worldPosition);
				if (dist > closestDistance)
					continue;
					
				closestDistance = dist;
				closestNode = i;
			}
		
			return closestNode;
		}
		
		private void calculateNeighbors(int nodeIndex, int3 endGridPosition)
		{
			var nodeGCost = GCosts[nodeIndex];

			var startIndex = nodeIndex * 26;

			for (var i = startIndex; i < startIndex + 26; i++)
			{
				var neighbor = Neighbors[i];
				if (!neighbor.Connects)
					continue;

				var neighborIndex = neighbor.Index;
				
				if (Availabilities[neighborIndex] != ENodeAvailabilityFlags.Available || SearchedNodes.Contains(neighborIndex))
					continue;
			
				var neighborNode = Nodes[neighborIndex];
				
				var gCost = nodeGCost + neighbor.Cost + neighborNode.AreaCost;
				if (gCost >= GCosts[neighborIndex])
					continue;
				
				var hCost = math.distance(neighborNode.GridPosition, endGridPosition);
				
				Connections[neighborIndex] = nodeIndex;
				GCosts[neighborIndex] = gCost;
				HCosts[neighborIndex] = hCost;
				FCosts[neighborIndex] = gCost + hCost;
			
				if (ToSearchNodes.Contains(neighborIndex))
					continue;

				if (ToSearchNodes.Length > 0 && hCost < HCosts[ToSearchNodes[0]])
				{
					ToSearchNodes.InsertRange(0, 1);
					ToSearchNodes[0] = neighborIndex;
				}
				else
				{
					ToSearchNodes.Add(neighborIndex);
				}
			}
		}
	}
}