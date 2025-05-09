//#define DEBUG_SPELL

using System;
using AI;
using AI.Interfaces;
using Combat.Casts.Interfaces;
using Combat.Enums;
using Combat.Spells.Interfaces;
using Managers;
using ScriptableObjects;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace Combat.Spells.Base
{
	public class BaseSpell : MonoBehaviour, ISpell
	{
		[field: SerializeField]
		public SpellData SpellData { get; set; }
		
		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID; 
			set
			{
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects.Remove(objectID);
				objectID = value;
				if (!string.IsNullOrWhiteSpace(objectID))
					StateManager.Instance.RegisteredObjects[objectID] = this;
			}
		}

		public IAlive Owner { get; set; }

		public Ray LastRay { get; private set; }
		public RaycastHit LastHit { get; private set; }

		public float OverrideRange { get; set; } = -1f;

		public bool IsCasting { get; private set; }
		public bool IsSelected { get; private set; }
		public bool IsOnCooldown => Time.time < LastFinishedCast + SpellData.Cooldown;

		public float LastStartedCast { get; private set; } = float.NegativeInfinity;
		public float LastFinishedCast { get; private set; } = float.NegativeInfinity;
		public float PredictFinishCast { get; private set; } = float.NegativeInfinity;
		
		private ICast cast;

		#region Identify / SaveLoad
		
		public void Awake()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects[ObjectID] = this;
		}

		public void OnDestroy()
		{
			if (!string.IsNullOrWhiteSpace(ObjectID))
				StateManager.Instance.RegisteredObjects.Remove(ObjectID);
		}
		
		#endregion

		public virtual void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			if (!IsCasting || !IsSelected || !Owner.IsAlive)
				return;

			if (Owner.CurrentMana < SpellData.CastingCost)
			{
				CancelCasting();
				return;
			}
			
			if (Time.time < PredictFinishCast)
				return;

			switch (Owner)
			{
				case Player:
				{
					if (SettingsManager.Instance.GetKeybind("keybinds-gameplay-attack").Item1.IsPressed())
					{
						FinishCasting();
					}
					else
					{
						CancelCasting();
					}
					break;
				}
				case NPC npc:
				{
					if (SpellData.IsResource || npc.AttackTarget != null)
						FinishCasting();
					break;
				}
				default:
					throw new NotImplementedException();
			}
		}

#if UNITY_EDITOR && DEBUG_SPELL
		public void OnDrawGizmos()
		{
			Gizmos.color = Color.red;
			
			Gizmos.DrawRay(LastRay.origin, LastRay.direction * SpellData.Range);
		}
#endif
		
		#region ISpell

		public void SetState(float cooldown, bool casting, float castedTime)
		{
			var time = Time.time;
			
			LastStartedCast = time - SpellData.Cooldown - SpellData.CastingTime;
			LastFinishedCast = time - SpellData.Cooldown + cooldown;

			if (casting)
			{
				StartCasting();

				PredictFinishCast -= castedTime;
			}
		}
		
		public virtual void Select()
		{
			Owner.Body.SetCoreGlow(SpellData.Element);
			IsSelected = true;
		}
		
		public virtual void Unselect()
		{
			Owner.Body.SetCoreGlow(EElement.Unknown);
			CancelCasting();
			
			IsSelected = false;
		}
		
		public virtual bool CanCast()
		{
			if (IsCasting || Owner.Paralyzed)
				return false;

			if (IsOnCooldown || Owner.CurrentMana < SpellData.CastingCost)
				return false;

			if (Owner is NPC npc && npc.SwitchCastCooldown > 0f && Time.time < npc.SwitchCastCooldown)
				return false;
				
			return true;
		}
		
		public virtual void StartCasting()
		{
			if (!CanCast())
				return;
			
			IsCasting = true;
			Owner.Body.SetCoreCenter(true);

			if (SpellData.LockWhileCasting)
				Owner.AddSlowSource(ObjectID, 1f, float.MaxValue);
			
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
			Owner.Body.SetCoreCenter(false);
			
			if (SpellData.LockAfterCasting != 0f)
				Owner.AddSlowSource(ObjectID, 1f, SpellData.LockAfterCasting);
			
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
				ObjectManager.Instance.CreateProjectile(SpellData.Projectile, OverrideRange < 0f ? Owner.SpellRange : OverrideRange, SpellData.Attack, this, LastRay.origin, LastRay.direction);
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
			Owner.Body.SetCoreCenter(false);
			
			if (SpellData.LockWhileCasting)
				Owner.RemoveSlowSource(ObjectID);
			
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
					var ray = player.Camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

					if (Physics.Raycast(ray, out var camHit, float.MaxValue, ~LayerMaskTools.GetMaskPlayer(), QueryTriggerInteraction.Ignore))
					{
						ray.origin = Owner.Body.Core.position;
						ray.direction = camHit.point - ray.origin;
					}
					
					LastRay = ray;
					break;
				case NPC npc:
					var ownerTr = Owner.GetTransform();
					var targetPos = calculatePrediction(npc);
					var verticalOffset = ownerTr.up * ((NPCData)npc.Data).AttackVerticalOffset;
					
					var direction = npc.AttackTarget == null ? ownerTr.forward : (targetPos - (ownerTr.position + verticalOffset)).normalized;
					LastRay = new Ray(Owner.Body.Core.position + verticalOffset, direction);
					break;
				default:
					throw new NotImplementedException();
			}

			var range = OverrideRange < 0f ? Owner.SpellRange : OverrideRange;
			
			// Hit did not land due to distance or other reasons. Try to fill the necessary data
			if (!Physics.Raycast(LastRay, out var hit, range, ~LayerMaskTools.GetMask(), QueryTriggerInteraction.Ignore))
			{
				hit.point = LastRay.origin + LastRay.direction * range;
				hit.normal = -LastRay.direction;
			}

			LastHit = hit;
		}

		private Vector3 calculatePrediction(NPC npc)
		{
			// No target, nothing to predict
			if (npc.AttackTarget == null)
				return Vector3.zero;
			
			var targetPos = npc.AttackTargetTransform.position;
			
			// No projectile, no need for prediction
			if (SpellData.Projectile == null)
				return targetPos;
			
			var ownerPos = npc.GetTransform().position;
			var npcData = (NPCData)npc.Data;

			// Only use prediction if close enough
			var distance = Vector3.Distance(ownerPos, targetPos);
			if (distance < npcData.TargetPredictMinimumRange)
				return targetPos;
			
			Vector3 velocity;
			
			// Velocity grabbing differs for NPCs
			switch (npc.AttackTarget)
			{
				case Player targetPlayer:
				{
					velocity = targetPlayer.Body.Rigidbody.linearVelocity;
					break;
				}
				case NPC targetNPC:
				{
					velocity = targetNPC.Velocity;
					break;
				}
				default:
					throw new NotImplementedException();
			}
			
			// Add some amount of fake velocity for extra inaccuracy
			if (velocity.magnitude < npcData.TargetPredictStartFakeVelocity)
			{
				var fakeVel = npcData.TargetPredictMaximumFakeVelocity;
				velocity = new Vector3(Random.Range(-fakeVel, fakeVel), Random.Range(-fakeVel, fakeVel), Random.Range(-fakeVel, fakeVel));
			}
			
			var distMul = npcData.TargetPredictDistanceMultiplier;
			var velMul = npcData.TargetPredictVelocityMultiplier;
			
			// Use distance
			var distInaccuracy = distMul * npcData.TargetPredictInaccuracy;
			distance *= distMul + Random.Range(-distInaccuracy, distInaccuracy);
			
			// Use velocity
			var velInaccuracy = velMul * npcData.TargetPredictInaccuracy;
			velocity *= velMul + Random.Range(-velInaccuracy, velInaccuracy);
			
			var prediction = velocity * distance;
			targetPos += prediction;
			
			return targetPos;
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