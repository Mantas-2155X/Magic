//#define DEBUG_BaseWeapon

using System;
using AI;
using AI.Interfaces;
using Managers;
using Objects;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseWeapon : MonoBehaviour, IWeapon
	{
		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }

		[field: SerializeField]
		public Collider[] Colliders { get; set; }
		
		[field: SerializeField]
		public virtual float TimeBetweenAttacks { get; private set; }
		
		[field: SerializeField]
		public virtual float CastingTime { get; private set; }

		[field: SerializeField]
		public virtual float ManaCost { get; private set; }

		public bool IsCasting { get; private set; }
		
		public Ray FinishedRay { get; private set; }

		public float LastStartedCast { get; private set; }
		public float LastFinishedCast { get; private set; }

		public virtual void Update()
		{
			if (!IsCasting || Owner == null || !Owner.IsAlive)
				return;

			if (Time.time < LastStartedCast + CastingTime)
				return;

			switch (Owner)
			{
				case Player player:
				{
					if (player.AttackAction.action.IsPressed())
						FinishCasting();
					break;
				}
				case NPC npc:
				{
					if (npc.Target != null)
						FinishCasting();
					break;
				}
				default:
					throw new NotImplementedException();
			}
		}

		public virtual void Take(IAlive alive)
		{
			if (alive == null || Owner != null)
				return;
			
			Owner = alive;

			Destroy(GetComponent<DroppedWeapon>());

			Rigidbody.isKinematic = true;
			Rigidbody.detectCollisions = false;
			Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			Rigidbody.interpolation = RigidbodyInterpolation.None;

			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = false;
			
			var tr = transform;
			tr.SetParent(Owner.Body.WeaponContainer);
			tr.localPosition = Vector3.zero;
			tr.localEulerAngles = Vector3.zero;
		}
		
		public virtual void Drop()
		{
			if (Owner == null)
				return;
			
			CancelCasting();
			
			var dropTr = Owner is Player player ? player.DropWeaponTr : Owner.Body.WeaponContainer;

			var movePos = dropTr.position + (Vector3.down * 0.1f) + (dropTr.right * 0.1f);
			var moveAng = dropTr.eulerAngles;

			var go = gameObject;
			
			var tr = transform;
			tr.SetParent(World.World.Instance.Dropped);
			tr.position = movePos;
			tr.eulerAngles = moveAng;
			
			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = true;

			go.AddComponent<DroppedWeapon>();
			
			Rigidbody.isKinematic = false;
			Rigidbody.detectCollisions = true;
			Rigidbody.constraints = RigidbodyConstraints.None;
			Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
			
			Rigidbody.MovePosition(movePos);
			Rigidbody.MoveRotation(Quaternion.Euler(moveAng));

			Owner = null;
		}
		
		public virtual bool CanCast()
		{
			if (IsCasting || Owner == null)
				return false;

			return Time.time >= LastFinishedCast + TimeBetweenAttacks && Owner.CurrentMana >= ManaCost;
		}
		
		public virtual void StartCasting()
		{
			if (!CanCast())
				return;

			IsCasting = true;
			LastStartedCast = Time.time;
		}
		
		public virtual void FinishCasting()
		{
			if (!IsCasting)
				return;

			IsCasting = false;
			LastFinishedCast = Time.time;

			CalculateRay();
			Owner.UseMana(ManaCost, this);
		}
		
		public virtual void CancelCasting()
		{
			if (!IsCasting)
				return;
			
			IsCasting = false;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		public void CalculateRay()
		{
			FinishedRay = default;
			
			switch (Owner)
			{
				case Player player:
					FinishedRay = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
					break;
				case NPC npc:
					var ownerTr = Owner.GetGameObject().transform;
					var direction = npc.AimLimited || npc.Target == null ? ownerTr.forward : (npc.Target.transform.position - ownerTr.position).normalized;
					FinishedRay = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public void OnDisable()
		{
			LastStartedCast = 0f;
			LastFinishedCast = 0f;
			IsCasting = false;
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}