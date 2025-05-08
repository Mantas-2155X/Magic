using UnityEngine;

namespace Tools
{
	public static class TransformTools
	{
		public static string GetFullPath(Transform transform)
		{
			var path = "/" + transform.name;
			
			while (transform.parent != null)
			{
				transform = transform.parent;
				path = "/" + transform.name + path;
			}

			return path;
		}
	}
}