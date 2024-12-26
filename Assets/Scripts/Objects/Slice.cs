using AI.Interfaces;
using Objects.Base;
using UnityEngine;

namespace Objects
{
	public class Slice : BaseObject
	{
		[SerializeField]
		public ParticleSystem System;

		private IAlive target;
		
		public override void Spawn(Vector3 position, Vector3 angles)
		{
			base.Spawn(position, angles);
			System.Play(true);
		}

		public override void OnDisable()
		{
			base.OnDisable();
			target = null;
		}

		public void SetTarget(IAlive alive)
		{
			target = alive;
		}

		public void Update()
		{
			if (target == null || !target.IsAlive)
				return;

			GetTransform().position = target.GetTransform().position;
		}
	}
}