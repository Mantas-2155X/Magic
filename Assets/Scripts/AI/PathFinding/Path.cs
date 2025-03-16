using AI.PathFinding.Structs;
using Unity.Collections;
using UnityEngine;

namespace AI.PathFinding
{
	public class Path
	{
		public Vector3[] Points { get; private set; }
		
		public Vector3[] Searched { get; private set; }
		
		public int[] Indexes { get; private set; }
		
		public float NodeRadius { get; private set; }
		
		public int Identifier { get; private set; }
		
		public static Path Create(NativeArray<SNode> nodes, NativeHashSet<int> searchedNodes, NativeList<int> resultingPath, float radius, int identifier)
		{
			var path = new Path
			{
				Points = new Vector3[resultingPath.Length],
				Searched = new Vector3[searchedNodes.Count],
				Indexes = new int[resultingPath.Length],
				NodeRadius = radius,
				Identifier = identifier
			};

			for (var i = 0; i < resultingPath.Length; i++)
			{
				var nodeIndex = resultingPath[(resultingPath.Length - 1) - i];
				
				path.Points[i] = nodes[nodeIndex].WorldPosition;
				path.Indexes[i] = nodeIndex;
			}

			var index = 0;
			foreach (var searchedNode in searchedNodes)
			{
				path.Searched[index] = nodes[searchedNode].WorldPosition;
				index++;
			}

			return path;
		}
	}
}