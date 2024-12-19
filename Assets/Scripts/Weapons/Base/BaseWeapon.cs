//#define DEBUG_BaseWeapon

using System;
using System.Runtime.CompilerServices;
using AI;
using AI.Interfaces;
using Casts.Interfaces;
using Managers;
using Objects;
using ScriptableObjects;
using Tools;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseWeapon : MonoBehaviour, IWeapon
	{
		[field: SerializeField]
		public WeaponData WeaponData { get; set; }

		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }
		[field: SerializeField]
		public Collider[] Colliders { get; set; }

		public bool IsCasting { get; private set; }
		
		public Ray LastRay { get; private set; }
		public RaycastHit LastHit { get; private set; }

		public float LastStartedCast { get; private set; } = float.NegativeInfinity;
		public float LastFinishedCast { get; private set; } = float.NegativeInfinity;

		private ICast currentCast;
		
		public Transform ownerTr;

		public virtual void Update()
		{
			if (!IsCasting || Owner == null || !Owner.IsAlive)
				return;

			if (Time.time < LastStartedCast + WeaponData.CastingTime)
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

			ownerTr = Owner.GetTransform();
			
			Destroy(GetComponent<DroppedWeapon>());

			Rigidbody.isKinematic = true;
			Rigidbody.detectCollisions = false;
			Rigidbody.constraints = RigidbodyConstraints.FreezeAll;
			Rigidbody.interpolation = RigidbodyInterpolation.None;

			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = false;
			
			var tr = GetTransform();
			
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

			var go = GetGameObject();
			var tr = GetTransform();
			
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

			return Time.time >= LastFinishedCast + WeaponData.Cooldown && Owner.CurrentMana >= WeaponData.ManaCost;
		}
		
		public virtual void StartCasting()
		{
			if (!CanCast())
				return;

			IsCasting = true;
			LastStartedCast = Time.time;
			
			clearCast();

			if (WeaponData.Cast != null)
				currentCast = ObjectManager.Instance.CreateCast(WeaponData.Cast, this);
		}
		
		public virtual bool FinishCasting()
		{
			if (!IsCasting)
				return false;

			IsCasting = false;
			LastFinishedCast = Time.time;

			Owner.UseMana(WeaponData.ManaCost, this);
			
			CalculateHit();
			clearCast();

			if (WeaponData.MaximumDistance != 0f && LastHit.distance > WeaponData.MaximumDistance)
				return false;

			if (WeaponData.Attack != null)
				ObjectManager.Instance.CreateAttack(WeaponData.Attack, this, LastHit, LastHit.transform);
			
			if (WeaponData.Projectile != null)
				ObjectManager.Instance.CreateProjectile(WeaponData.Projectile, this, LastRay.origin, LastRay.direction);
			
			return true;
		}
		
		public virtual void CancelCasting()
		{
			if (!IsCasting)
				return;
			
			IsCasting = false;
			clearCast();
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => gameObject;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => transform;

		public void CalculateHit()
		{
			LastRay = default;
			LastHit = default;
			
			switch (Owner)
			{
				case Player player:
					LastRay = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
					break;
				case NPC npc:
					var direction = npc.Target == null ? ownerTr.forward : (npc.TargetTransform.position - ownerTr.position).normalized;
					LastRay = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);
					break;
				default:
					throw new NotImplementedException();
			}

			Physics.Raycast(LastRay, out var hit, float.MaxValue, ~LayerMaskTools.GetMask());
			LastHit = hit;
		}

		public void OnDisable()
		{
			clearCast();
			LastStartedCast = float.NegativeInfinity;
			LastFinishedCast = float.NegativeInfinity;
			IsCasting = false;
			PoolingManager.Instance.AddToPool(WeaponData, gameObject);
		}

		private void clearCast()
		{
			if (currentCast == null)
				return;

			currentCast.StopParticles();
			currentCast = null;
		}
	}
}