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
		public NativeArray<float> Areas;

		[ReadOnly]
		public NativeArray<ENodeAvailability> Availabilities;

		public NativeArray<float> GCosts;
		public NativeArray<float> HCosts;
		public NativeArray<float> FCosts;
		public NativeArray<int> Connections;
		
		[WriteOnly]
		public NativeList<int> ResultingPath;

		public NativeHashSet<int> SearchedNodes;
		public NativeList<int> ToSearchNodes;
		
		[ReadOnly]
		public float Distance;
		
		[ReadOnly]
		public float3 StartPosition;
		
		[ReadOnly]
		public float3 EndPosition;
		
		[ReadOnly]
		public NativeArray<bool> Obstructed;
		
		public void Execute()
		{
			SearchedNodes.Clear();
			ToSearchNodes.Clear();
			
			ResultingPath.Clear();
			
			var startNodeIndex = findClosestNode(StartPosition);
			var endNodeIndex = findClosestNode(EndPosition);

			var distanceBetweenPoints = math.distance(StartPosition, EndPosition) / Distance;

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
			
				calculateNeighbors(nodeIndex, EndPosition);
			}
		}

		private int findClosestNode(float3 worldPosition)
		{
			var closestDistance = Mathf.Infinity;
			var closestNode = -1;

			for (var i = 0; i < Nodes.Length; i++)
			{
				if (Availabilities[i] != ENodeAvailability.Available || Obstructed[i])
					continue;

				var dist = math.distancesq(Nodes[i].WorldPosition, worldPosition);
				if (dist > closestDistance)
					continue;
					
				closestDistance = dist;
				closestNode = i;
			}
		
			return closestNode;
		}
		
		private void calculateNeighbors(int nodeIndex, float3 endPosition)
		{
			var nodeGCost = GCosts[nodeIndex];

			var startIndex = nodeIndex * 26;

			for (var i = startIndex; i < startIndex + 26; i++)
			{
				var neighbor = Neighbors[i];
				if (!neighbor.Valid)
					continue;

				var neighborIndex = neighbor.Index;
				
				if (Availabilities[neighborIndex] != ENodeAvailability.Available || Obstructed[neighborIndex])
					continue;
			
				if (SearchedNodes.Contains(neighborIndex))
					continue;
				
				var gCost = nodeGCost + neighbor.Cost + Areas[neighborIndex];
				if (gCost >= GCosts[neighborIndex])
					continue;
				
				var neighborPos = Nodes[neighborIndex].WorldPosition;
				var hCost = math.distance(neighborPos, endPosition) / Distance;
				
				Connections[neighborIndex] = nodeIndex;
				GCosts[neighborIndex] = gCost;
				HCosts[neighborIndex] = hCost;
				FCosts[neighborIndex] = gCost + hCost;
			
				if (!ToSearchNodes.Contains(neighborIndex))
					ToSearchNodes.Add(neighborIndex);
			}
		}
	}
}