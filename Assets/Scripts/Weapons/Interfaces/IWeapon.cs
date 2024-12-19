using AI.Interfaces;
using Objects;
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
		public DroppedWeapon DroppedWeapon { get; }

		public bool IsCasting { get; }
		
		public Ray LastRay { get; }
		public RaycastHit LastHit { get; }

		public float LastStartedCast { get; }
		public float LastFinishedCast { get; }

		public void Spawn(Vector3 position, Vector3 angles);

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