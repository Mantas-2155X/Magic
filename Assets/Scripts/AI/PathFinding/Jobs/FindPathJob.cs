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
		public NativeArray<SNode> Nodes;
		
		[ReadOnly]
		public NativeArray<SIndexWithCost> Neighbors;
		
		[WriteOnly]
		public NativeList<SNode> ResultingPath;

		public NativeHashSet<int> SearchedNodes;
		public NativeList<int> ToSearchNodes;
		
		[ReadOnly]
		public float Distance;
		
		[ReadOnly]
		public float3 StartPosition;
		
		[ReadOnly]
		public float3 EndPosition;
		
		public void Execute()
		{
			SearchedNodes.Clear();
			ToSearchNodes.Clear();
			
			ResultingPath.Clear();
			
			var distanceBetweenPoints = math.distance(StartPosition, EndPosition) / Distance;
			
			var startNodeIndex = findClosestNode(StartPosition);
			
			var startNode = Nodes[startNodeIndex];
			startNode.GCost = 0f;
			startNode.HCost = distanceBetweenPoints;
			startNode.FCost = distanceBetweenPoints;
			Nodes[startNodeIndex] = startNode;
			
			var endNodeIndex = findClosestNode(EndPosition);

			ToSearchNodes.Add(startNodeIndex);
			
			while (ToSearchNodes.Length > 0)
			{
				var nodeIndex = ToSearchNodes[0];
				var node = Nodes[nodeIndex];
				
				for (var i = 0; i < ToSearchNodes.Length; i++)
				{
					var searchingNodeIndex = ToSearchNodes[i];
					var searchingNode = Nodes[searchingNodeIndex];
					
					if (searchingNode.FCost < node.FCost || searchingNode.FCost == node.FCost && searchingNode.HCost < node.HCost)
						nodeIndex = searchingNodeIndex;
				}

				ToSearchNodes.RemoveAt(ToSearchNodes.IndexOf(nodeIndex));
				SearchedNodes.Add(nodeIndex);

				if (nodeIndex == endNodeIndex)
				{
					while (endNodeIndex != startNodeIndex)
					{
						var endNode = Nodes[endNodeIndex];
						
						ResultingPath.Add(endNode);
						endNodeIndex = endNode.Connection;
					}
		
					ResultingPath.Add(startNode);
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
			var node = Nodes[nodeIndex];

			var startIndex = nodeIndex * 26;

			for (var i = startIndex; i < startIndex + 26; i++)
			{
				var neighbor = Neighbors[i];
				if (!neighbor.Valid)
					continue;

				var neighborIndex = neighbor.Index;
				
				var neighborNode = Nodes[neighborIndex];
				if (neighborNode.Availability != ENodeAvailabilityFlags.Available)
					continue;
			
				if (SearchedNodes.Contains(neighborIndex))
					continue;
				
				var gCost = node.GCost + neighbor.Cost;
				if (gCost >= neighborNode.GCost)
					continue;
				
				var neighborPos = neighborNode.WorldPosition;
				var hCost = math.distance(neighborPos, endPosition) / Distance;
				
				neighborNode.Connection = nodeIndex;
				neighborNode.GCost = gCost;
				neighborNode.HCost = hCost;
				neighborNode.FCost = gCost + hCost;
				Nodes[neighborIndex] = neighborNode;
			
				if (!ToSearchNodes.Contains(neighborIndex))
					ToSearchNodes.Add(neighborIndex);
			}
		}
	}
}