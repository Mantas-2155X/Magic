using System;
using AI.Interfaces;
using UnityEngine;

namespace Weapons.Interfaces
{
	public interface IWeapon
	{
		public IAlive Owner { get; }
		
		public Rigidbody Rigidbody { get; }
		public Collider[] Colliders { get; }

		public float TimeBetweenAttacks { get; }
		public float CastingTime { get; }
		public float ManaCost { get; }

		public Type Cast { get; }

		public bool IsCasting { get; }
		
		public Ray LastRay { get; }
		public RaycastHit LastHit { get; }

		public float LastStartedCast { get; }
		public float LastFinishedCast { get; }

		public void Take(IAlive alive);
		public void Drop();
		
		public bool CanCast();
		public void StartCasting();
		public void FinishCasting();
		public void CancelCasting();
		
		public GameObject GetGameObject();
	}
}