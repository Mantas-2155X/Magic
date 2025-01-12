using System.Collections.Generic;
using UnityEngine;

namespace ScriptableObjects
{
	[CreateAssetMenu]
	public class Path : ScriptableObject
	{
		[SerializeField]
		public List<Vector3> Points = new ();

		public static Path FromVectors(List<Vector3> points)
		{
			var path = CreateInstance<Path>();
			path.Points = points;
			
			return path;
		}
	}
}