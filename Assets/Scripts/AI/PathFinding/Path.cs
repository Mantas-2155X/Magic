using UnityEngine;

namespace AI.PathFinding
{
	public class Path
	{
		public Vector3[] Points { get; }
		
		public int SearchedNodes { get; }

		public Path(Vector3[] points, int searchedNodes)
		{
			Points = points;
			SearchedNodes = searchedNodes;
		}
	}
}