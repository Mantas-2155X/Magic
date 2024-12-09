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
		public Collider[] Colliders { get; set; }
		
		[field: SerializeField]
		public virtual float Force { get; private set; }
		public virtual Type Projectile { get; private set; }
		[field: SerializeField]
		public virtual float TimeBetweenAttacks { get; private set; }
		
		public Ray Ray { get; private set; }
		public float LastAttackTime { get; private set; }

		public virtual void Take(IAlive alive)
		{
			if (alive == null || Owner != null)
				return;
			
			Owner = alive;
			
			Destroy(GetComponent<DroppedWeapon>());
			Destroy(GetComponent<Rigidbody>());

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
			{
				Destroy(gameObject);
				return;
			}
			
			var go = gameObject;
			
			var tr = transform;
			tr.SetParent(World.World.Instance.Dropped);
			tr.position = Owner.Body.WeaponContainer.position + Vector3.down * 0.1f;
			tr.eulerAngles = Owner.Body.WeaponContainer.eulerAngles;
			
			for (var i = 0; i < Colliders.Length; i++)
				Colliders[i].enabled = true;

			go.AddComponent<DroppedWeapon>();
			
			var rb = go.AddComponent<Rigidbody>();
			rb.interpolation = RigidbodyInterpolation.Interpolate;
			rb.mass = 5f;

			Owner = null;
		}
		
		public virtual bool CanAttack()
		{
			if (Owner == null)
				return false;

			return Time.time >= LastAttackTime + TimeBetweenAttacks;
		}
		
		public virtual bool Attack()
		{
			if (!CanAttack())
				return false;

			CalculateRay();
			
			LastAttackTime = Time.time;
			return true;
		}
		
		public GameObject GetGameObject()
		{
			return gameObject;
		}

		public void CalculateRay()
		{
			Ray = default;
			
			switch (Owner)
			{
				case Player player:
					Ray = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
					break;
				case NPC npc:
					var ownerTr = Owner.GetGameObject().transform;
					var direction = npc.AimLimited || npc.Target == null ? ownerTr.forward : (npc.Target.transform.position - ownerTr.position).normalized;
					Ray = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);
					break;
				default:
					throw new NotImplementedException();
			}
		}

		public void OnDisable()
		{
			LastAttackTime = 0f;
			PoolingManager.Instance.AddToPool(GetType(), gameObject);
		}
	}
}