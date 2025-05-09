using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class Portal : BaseObject
	{
		public override bool ShouldSave => false;
		
		[SerializeField]
		public ParticleSystem System;

		public override void Spawn(Vector3 position, Vector3 angles)
		{
			base.Spawn(position, angles);
			System.Play(true);
		}
	}
}