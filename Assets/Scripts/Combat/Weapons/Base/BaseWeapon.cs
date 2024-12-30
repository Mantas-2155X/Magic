using System.Runtime.CompilerServices;
using AI;
using AI.Interfaces;
using Combat.Weapons.Interfaces;
using Managers;
using Objects;
using ScriptableObjects;
using UnityEngine;

namespace Combat.Weapons.Base
{
	public class BaseWeapon : MonoBehaviour, IWeapon
	{
		[field: SerializeField]
		public WeaponData WeaponData { get; private set; }

		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }
		[field: SerializeField]
		public Collider[] Colliders { get; set; }
		[field: SerializeField]
		public DroppedWeapon DroppedWeapon { get; set; }

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		public void Awake()
		{
			initializeObject();
			
			DroppedWeapon.Weapon = this;
		}

		public void OnDisable()
		{
			PoolingManager.Instance.Add(WeaponData, gameObject);
		}

		public virtual void Spawn(Vector3 position, Vector3 angles)
		{
			initializeObject();
			
			thisTr.position = position;
			thisTr.eulerAngles = angles;
			
			thisGo.SetActive(true);
		}
		
		public virtual void Take(IAlive alive)
		{
			if (alive == null || Owner != null)
				return;
			
			Owner = alive;

			DroppedWeapon.enabled = false;

			Rigidbody.isKinematic = true;
			Rigidbody.detectCollisions = false;
			Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			Rigidbody.interpolation = RigidbodyInterpolation.None;

			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = false;
			
			thisTr.SetParent(Owner.Body.WeaponContainer);
			thisTr.localPosition = Vector3.zero;
			thisTr.localEulerAngles = Vector3.zero;
		}
		
		public virtual void Drop()
		{
			if (Owner == null)
				return;
			
			var dropTr = Owner is Player player ? player.DropWeaponTr : Owner.Body.WeaponContainer;

			var movePos = dropTr.position + (Vector3.down * 0.1f) + (dropTr.right * 0.1f);
			var moveAng = dropTr.eulerAngles;
			
			thisTr.SetParent(World.World.Instance.Objects);
			thisTr.position = movePos;
			thisTr.eulerAngles = moveAng;
			
			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = true;
			
			Rigidbody.isKinematic = false;
			Rigidbody.detectCollisions = true;
			Rigidbody.constraints = RigidbodyConstraints.None;
			Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			
			Rigidbody.MovePosition(movePos);
			Rigidbody.MoveRotation(Quaternion.Euler(moveAng));

			DroppedWeapon.enabled = true;

			Owner = null;
		}

		public IAlive GetAlive()
		{
			return Owner;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;

		private void initializeObject()
		{
			if (init)
				return;

			thisGo = gameObject;
			thisTr = thisGo.transform;
			init = true;
		}
	}
}