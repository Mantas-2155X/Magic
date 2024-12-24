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
		public WeaponData WeaponData { get; private set; }

		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public Rigidbody Rigidbody { get; set; }
		[field: SerializeField]
		public Collider[] Colliders { get; set; }
		[field: SerializeField]
		public DroppedWeapon DroppedWeapon { get; set; }

		public bool IsCasting { get; private set; }
		
		public Ray LastRay { get; private set; }
		public RaycastHit LastHit { get; private set; }

		public float LastStartedCast { get; private set; } = float.NegativeInfinity;
		public float LastFinishedCast { get; private set; } = float.NegativeInfinity;
		public float PredictFinishCast { get; private set; } = float.NegativeInfinity;

		private ICast currentCast;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;

		public void Awake()
		{
			initializeObject();
			
			DroppedWeapon.Weapon = this;
		}
		
		public virtual void Update()
		{
			if (!IsCasting || Owner == null || !Owner.IsAlive)
				return;

			if (Time.time < PredictFinishCast)
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
					if (npc.AttackTarget != null)
						FinishCasting();
					break;
				}
				default:
					throw new NotImplementedException();
			}
		}

		public void OnDisable()
		{
			clearCast();
			LastStartedCast = float.NegativeInfinity;
			LastFinishedCast = float.NegativeInfinity;
			PredictFinishCast = float.NegativeInfinity;
			IsCasting = false;
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
			
			CancelCasting();
			
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
		
		public virtual bool CanCast()
		{
			if (IsCasting || Owner == null)
				return false;

			return Time.time >= LastFinishedCast + WeaponData.Cooldown && Owner.CurrentMana >= WeaponData.CastingCost;
		}
		
		public virtual void StartCasting()
		{
			if (!CanCast())
				return;

			IsCasting = true;
			LastStartedCast = Time.time;
			PredictFinishCast = LastStartedCast + WeaponData.CastingTime;
			
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

			Owner.UseMana(WeaponData.CastingCost, this);
			
			calculateHit();
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
		
		private void calculateHit()
		{
			LastRay = default;
			LastHit = default;

			var ownerTr = Owner.GetTransform();
			
			switch (Owner)
			{
				case Player player:
					LastRay = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
					break;
				case NPC npc:
					var direction = npc.AttackTarget == null ? ownerTr.forward : (npc.AttackTargetTransform.position - ownerTr.position).normalized;
					LastRay = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);
					break;
				default:
					throw new NotImplementedException();
			}

			Physics.Raycast(LastRay, out var hit, float.MaxValue, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore);
			LastHit = hit;
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