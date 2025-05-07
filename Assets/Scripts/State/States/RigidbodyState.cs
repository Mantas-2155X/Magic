using Newtonsoft.Json;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class RigidbodyState
	{
		[JsonProperty]
		public Vector3 Position;

		[JsonProperty]
		public Quaternion Rotation;

		[JsonProperty]
		public Vector3 LinearVelocity;
		
		[JsonProperty]
		public Vector3 AngularVelocity;
		
		public static RigidbodyState Read(Rigidbody rigidbody)
		{
			if (rigidbody == null)
				return null;
			
			return new RigidbodyState
			{
				Position = rigidbody.position,
				Rotation = rigidbody.rotation,
				LinearVelocity = rigidbody.linearVelocity,
				AngularVelocity = rigidbody.angularVelocity,
			};
		}

		public static void Apply(Rigidbody rigidbody, RigidbodyState state)
		{
			if (rigidbody == null)
				return;
			
			rigidbody.position = state.Position;
			rigidbody.rotation = state.Rotation;

			if (!rigidbody.isKinematic)
			{
				rigidbody.linearVelocity = state.LinearVelocity;
				rigidbody.angularVelocity = state.AngularVelocity;
			}
		}
	}
}