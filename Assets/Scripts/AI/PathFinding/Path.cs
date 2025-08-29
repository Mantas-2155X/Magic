using System.Collections.Generic;
using AI.PathFinding.Structs;
using Unity.Collections;
using UnityEngine;

namespace AI.PathFinding
{
	public class Path
	{
		public List<Vector3> Points { get; private set; }
		
		public List<Vector3> Searched { get; private set; }
		
		public List<int> Indexes { get; private set; }
		
		public float NodeRadius { get; private set; }
		
		public int Identifier { get; private set; }
		
		public static Path Create(NativeArray<SNode> nodes, NativeHashSet<int> searchedNodes, NativeList<int> resultingPath, float radius, int identifier, Vector3 startPosition, Vector3 endPosition)
		{
			if (!searchedNodes.IsCreated || !resultingPath.IsCreated) 
				return null;
			
			var path = new Path
			{
				Points = new List<Vector3>(),
				Searched = new List<Vector3>(),
				Indexes = new List<int>(),
				NodeRadius = radius,
				Identifier = identifier
			};

			var pathLength = resultingPath.Length;
			for (var i = 0; i < pathLength; i++)
			{
				var nodeIndex = resultingPath[(pathLength - 1) - i];
				
				path.Points.Add(nodes[nodeIndex].WorldPosition);
				path.Indexes.Add(nodeIndex);
			}

			foreach (var searchedNode in searchedNodes)
				path.Searched.Add(nodes[searchedNode].WorldPosition);

			if (path.Points.Count > 0)
			{
				// Set the first point as the start position instead of closest node to avoid micro-adjustment
				path.Points[0] = startPosition;
				
				// End path is the closest node so add another point which is the exact end position
				path.Points.Add(endPosition);
				path.Indexes.Add(path.Indexes[^1]);
			}
			
			return path;
		}
	}
}