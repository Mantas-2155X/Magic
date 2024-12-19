using AI.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace Weapons.Interfaces
{
	public interface IWeapon
	{
		public WeaponData WeaponData { get; }
		
		public IAlive Owner { get; }
		
		public Rigidbody Rigidbody { get; }
		public Collider[] Colliders { get; }

		public bool IsCasting { get; }
		
		public Ray LastRay { get; }
		public RaycastHit LastHit { get; }

		public float LastStartedCast { get; }
		public float LastFinishedCast { get; }

		public void Take(IAlive alive);
		public void Drop();
		
		public bool CanCast();
		public void StartCasting();
		public bool FinishCasting();
		public void CancelCasting();
		
		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}