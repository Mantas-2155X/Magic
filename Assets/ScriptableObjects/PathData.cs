using System;
using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class PathData : Data
	{
		[SerializeField]
		public List<SPathPoint> Points = new ();

		public static PathData FromVectors(List<Vector3> points, List<float> pauses = null)
		{
			var path = CreateInstance<PathData>();
			path.Points = new List<SPathPoint>();

			for (var i = 0; i < points.Count; i++)
			{
				var point = new SPathPoint();
				point.Point = points[i];
				
				if (pauses != null && pauses.Count > i)
					point.Pause = pauses[i];
				
				path.Points.Add(point);
			}
			
			return path;
		}

		[Serializable]
		public struct SPathPoint
		{
			[SerializeField]
			public Vector3 Point;
			
			[SerializeField]
			public float Pause;
		}
	}
}