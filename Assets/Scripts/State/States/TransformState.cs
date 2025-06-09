using Newtonsoft.Json;
using State.Interfaces;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class TransformState : IState
	{
		[JsonProperty]
		public Vector3 Position;

		[JsonProperty]
		public Quaternion Rotation;
		
		[JsonProperty]
		public Vector3 Scale;

		public TransformState() { }
		
		public TransformState(object obj)
		{
			Read(obj);
		}

		public void Read(object obj)
		{
			if (obj is not Transform transform)
				return;

			Position = transform.position;
			Rotation = transform.rotation;
			Scale = transform.localScale;
		}
			
		public void Apply(object obj)
		{
			if (obj is not Transform transform)
				return;

			transform.position = Position;
			transform.rotation = Rotation;
			transform.localScale = Scale;
		}
	}
}