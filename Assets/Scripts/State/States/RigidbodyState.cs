using Newtonsoft.Json;
using State.Interfaces;
using UnityEngine;

namespace State.States
{
	[JsonObject]
	public class RigidbodyState : IState
	{
		[JsonProperty]
		public Vector3 Position;

		[JsonProperty]
		public Quaternion Rotation;

		[JsonProperty]
		public Vector3 LinearVelocity;
		
		[JsonProperty]
		public Vector3 AngularVelocity;
		
		public RigidbodyState() { }
		
		public RigidbodyState(object obj)
		{
			Read(obj);
		}
		
		public void Read(object obj)
		{
			if (obj is not Rigidbody rigidbody)
				return;

			Position = rigidbody.position;
			Rotation = rigidbody.rotation;
			LinearVelocity = rigidbody.linearVelocity;
			AngularVelocity = rigidbody.angularVelocity;
		}
			
		public void Apply(object obj)
		{
			if (obj is not Rigidbody rigidbody)
				return;

			rigidbody.position = Position;
			rigidbody.rotation = Rotation;

			if (!rigidbody.isKinematic)
			{
				rigidbody.linearVelocity = LinearVelocity;
				rigidbody.angularVelocity = AngularVelocity;
			}
		}
	}
}