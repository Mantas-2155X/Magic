using UnityEngine;

namespace World
{
	public class World5 : World
	{
		public override void Awake()
		{
			base.Awake();

			var cam = Camera.main;
			if (cam == null)
				return;

			cam.backgroundColor = Color.black;
			cam.clearFlags = CameraClearFlags.Color;
		}
	}
}