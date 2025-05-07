using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class TransformState
	{
		[JsonProperty]
		public Vector3 Position;

		[JsonProperty]
		public Quaternion Rotation;
		
		[JsonProperty]
		public Vector3 Scale;

		public static TransformState Read(Transform transform)
		{
			if (transform == null)
				return null;

			return new TransformState
			{
				Position = transform.position,
				Rotation = transform.rotation,
				Scale = transform.localScale
			};
		}

		public static void Apply(Transform transform, TransformState state)
		{
			if (transform == null)
				return;

			transform.position = state.Position;
			transform.rotation = state.Rotation;
			transform.localScale = state.Scale;
		}
	}
}