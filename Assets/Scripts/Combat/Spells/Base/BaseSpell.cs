using System;
using AI;
using AI.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Spells.Interfaces;
using Managers;
using ScriptableObjects;
using Tools;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Combat.Spells.Base
{
	public class BaseSpell : MonoBehaviour, ISpell
	{
		[field: SerializeField]
		public SpellData SpellData { get; set; }
		
		public IAlive Owner { get; set; }

		public Ray LastRay { get; private set; }
		public RaycastHit LastHit { get; private set; }

		public bool IsCasting { get; private set; }
		public bool IsSelected { get; private set; }
		public bool IsOnCooldown => Time.time < LastFinishedCast + SpellData.Cooldown;

		public float LastStartedCast { get; private set; } = float.NegativeInfinity;
		public float LastFinishedCast { get; private set; } = float.NegativeInfinity;
		public float PredictFinishCast { get; private set; } = float.NegativeInfinity;
		
		private ICast cast;

		#region MonoBehaviour

		public virtual void Update()
		{
			if (!IsCasting || !IsSelected || !Owner.IsAlive)
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

		#endregion

		#region ISpell
		
		public virtual void Select()
		{
			IsSelected = true;
		}
		
		public virtual void Unselect()
		{
			CancelCasting();
			
			IsSelected = false;
		}
		
		public virtual bool CanCast()
		{
			if (IsCasting || !IsSelected)
				return false;

			return !IsOnCooldown && Owner.CurrentMana >= SpellData.CastingCost;
		}
		
		public virtual void StartCasting()
		{
			if (!CanCast())
				return;
			
			IsCasting = true;
			LastStartedCast = Time.time;
			PredictFinishCast = LastStartedCast + SpellData.CastingTime;
			
			clearCast();

			if (SpellData.Cast != null)
				cast = ObjectManager.Instance.CreateCast(SpellData.Cast, this);
		}
		
		public virtual bool FinishCasting()
		{
			if (!IsCasting || !IsSelected)
				return false;

			IsCasting = false;
			LastFinishedCast = Time.time;

			// Add a variation to npc spell cooldowns
			if (Owner is NPC npc)
			{
				var extra = SpellData.Cooldown * Random.Range(0f, ((NPCData)npc.Data).SpellCooldownVariation);
				LastFinishedCast += extra;
			}
			
			Owner.TakeMana(SpellData.CastingCost, this);
			
			calculateHit();
			clearCast();
			
			if (SpellData.Projectile != null)
			{
				// Create a projectile which will create the attack on impact
				ObjectManager.Instance.CreateProjectile(SpellData.Projectile, Owner.SpellRange, SpellData.Attack, this, LastRay.origin, LastRay.direction);
			}
			else if (SpellData.Attack != null)
			{
				// No projectile, create the attack straight away
				ObjectManager.Instance.CreateAttack(SpellData.Attack, this, LastHit, LastHit.transform);
			}
			
			return true;
		}
		
		public virtual void CancelCasting()
		{
			if (!IsCasting)
				return;
			
			IsCasting = false;
			clearCast();
		}

		#endregion

		#region Internal

		private void calculateHit()
		{
			LastRay = default;
			LastHit = default;
			
			switch (Owner)
			{
				case Player player:
					LastRay = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
					break;
				case NPC npc:
					var ownerTr = Owner.GetTransform();
					var direction = npc.AttackTarget == null ? ownerTr.forward : (npc.AttackTargetTransform.position - ownerTr.position).normalized;
					LastRay = new Ray(ownerTr.position + ownerTr.up * 0.5f, direction);
					break;
				default:
					throw new NotImplementedException();
			}

			// Hit did not land due to distance or other reasons. Try to fill the necessary data
			if (!Physics.Raycast(LastRay, out var hit, Owner.SpellRange, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
			{
				hit.point = LastRay.origin + LastRay.direction * Owner.SpellRange;
				hit.normal = -LastRay.direction;
			}

			LastHit = hit;
		}

		private void clearCast()
		{
			if (cast == null)
				return;

			cast.StopParticles();
			cast = null;
		}

		#endregion
	}
}