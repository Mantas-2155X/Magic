using System;
using AI;
using AI.Interfaces;
using Objects;
using Tools;
using UnityEngine;
using Weapons.Interfaces;

namespace Weapons.Base
{
	public class BaseWeapon : MonoBehaviour, IWeapon
	{
		public IAlive Owner { get; private set; }
		
		[field: SerializeField]
		public virtual float Force { get; private set; }
		[field: SerializeField]
		public virtual string Projectile { get; private set; }
		[field: SerializeField]
		public virtual float TimeBetweenAttacks { get; private set; }
		
		public Ray Ray { get; private set; }
		public float LastAttackTime { get; private set; }

		public virtual void Take(IAlive alive)
		{
			if (alive == null || Owner != null)
				return;
			
			Owner = alive;

			var previousAngles = transform.localEulerAngles;
			
			transform.SetParent(Owner.Body.WeaponContainer, true);
			
			transform.localEulerAngles = previousAngles;
			transform.localPosition = Vector3.zero;
		}
		
		public virtual void Drop()
		{
			if (Owner == null)
			{
				Destroy(gameObject);
				return;
			}
			
			var addPos = Vector3.zero;
			var ownerTr = Owner.GetGameObject().transform;
			
			if (Physics.Raycast(new Ray(ownerTr.position, ownerTr.forward), 1f, ~LayerMaskTools.Mask2))
			{
				Debug.Log($"[BaseWeapon {Owner.GetGameObject().name}] Too close to drop forward");
			}
			else
			{
				addPos = ownerTr.forward * 0.65f;
			}
			
			var go = Instantiate(Resources.Load<GameObject>("Objects/DroppedWeapon"));
			
			var tr = go.transform;
			tr.SetParent(World.World.Instance.Dropped);

			tr.position = ownerTr.position + addPos;
			tr.eulerAngles = ownerTr.eulerAngles;
			
			var rb = go.GetComponent<Rigidbody>();
			rb.MovePosition(ownerTr.position + addPos);

			var dropped = go.GetComponent<DroppedWeapon>();
			dropped.Weapon = GetType().Name;
			
			Destroy(gameObject);
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
	}
}