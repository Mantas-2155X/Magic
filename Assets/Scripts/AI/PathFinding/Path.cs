using System.Collections.Generic;
using AI.PathFinding.Structs;
using Unity.Collections;
using UnityEngine;

namespace AI.PathFinding
{
	public class Path
	{
		public Vector3[] Points { get; private set; }
		
		public Vector3[] Searched { get; private set; }
		
		public List<Vector3> Obstructed { get; private set; }

		public float NodeRadius { get; private set; }
		
		public static Path Create(NativeArray<SNode> nodes, NativeHashSet<int> searchedNodes, NativeList<int> resultingPath, NativeArray<bool> obstructed, float radius)
		{
			if (resultingPath.Length == 0)
				return null;

			var path = new Path
			{
				Points = new Vector3[resultingPath.Length],
				Searched = new Vector3[searchedNodes.Count],
				Obstructed = new List<Vector3>(),
				NodeRadius = radius
			};

			for (var i = 0; i < resultingPath.Length; i++)
				path.Points[i] = nodes[resultingPath[i]].WorldPosition;

			var index = 0;
			foreach (var searchedNode in searchedNodes)
			{
				path.Searched[index] = nodes[searchedNode].WorldPosition;
				index++;
			}
			
			for (var i = 0; i < obstructed.Length; i++)
			{
				var obstructedNode = obstructed[i];
				if (!obstructedNode)
					continue;
				
				path.Obstructed.Add(nodes[i].WorldPosition);
			}

			return path;
		}
	}
}