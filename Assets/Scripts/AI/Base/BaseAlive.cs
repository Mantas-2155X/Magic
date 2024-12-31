#define BODY_GIB

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Enums;
using AI.Events;
using AI.Interfaces;
using Combat.Enums;
using Combat.Spells.Base;
using Combat.Spells.Interfaces;
using Combat.Wearables.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.Base
{
	public class BaseAlive : MonoBehaviour, IAlive
	{
		public static readonly OnHealEvent OnHealEvent = new ();
		public static readonly OnDamageEvent OnDamageEvent = new ();
		public static readonly OnManaGenerateEvent OnManaGenerateEvent = new ();
		public static readonly OnManaUseEvent OnManaUseEvent = new ();
		public static readonly OnDeathEvent OnDeathEvent = new ();
		public static readonly OnSpawnEvent OnSpawnEvent = new ();
		public static readonly OnRelationshipGroupChangedEvent OnRelationshipGroupChangedEvent = new ();
		
		private LayerMask previousExcludeLayers;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		#region MonoBehaviour

		public void OnCollisionEnter(Collision coll)
		{
			if (!IsAlive)
				return;

			var velocity = coll.relativeVelocity.y - Body.FallMinimumVelocity;
			if (velocity < 0f)
				return;

			var damage = Mathf.FloorToInt(Body.FallDamageMultiplier * (velocity * velocity));
			Damage(damage, null, EDamageType.World);
		}

		#endregion
		
		#region IAlive

		[field: SerializeField]
		public Body Body { get; private set; }
		
		public List<IWearable> Wearables { get; private set; }
		public List<ISpell> Spells { get; private set; }

		public ISpell Spell { get; private set; }

		public virtual float CurrentSpeed { get; private set; }
		public float MaximumSpeed { get; private set; }

		public float CurrentHealth { get; private set; }
		public float StartingHealth { get; private set; }
		public float OverloadHealth { get; private set; }
		public float RegenerateHealth { get; private set; }
		
		public float CurrentMana { get; private set; }
		public float StartingMana { get; private set; }
		public float OverloadMana { get; private set; }
		public float RegenerateMana { get; private set; }

		public EMovementType MovementType { get; private set; }
		public int RelationshipGroup { get; private set; }
		public float SpellRange { get; private set; }

		public bool IsAlive { get; private set; }
		public bool IsInvulnerable { get; private set; }
		public bool IsPowerful { get; private set; }
		public virtual bool IsWalking { get; private set; }

		public void SetInvulnerable(bool value)
		{
			if (!IsAlive || IsInvulnerable == value)
				return;
			
			IsInvulnerable = value;
		}
		public void SetPowerful(bool value)
		{
			if (!IsAlive || IsPowerful == value)
				return;
			
			IsPowerful = value;
		}
		public virtual void SetMovementType(EMovementType value)
		{
			if (!IsAlive || MovementType == value)
				return;
			
			MovementType = value;

			Body.Rigidbody.useGravity = MovementType == EMovementType.Normal;
			Body.BodyCollider.enabled = MovementType == EMovementType.Normal;
			
			if (MovementType != EMovementType.Normal)
				previousExcludeLayers = Body.Rigidbody.excludeLayers;
			else
				Body.Rigidbody.excludeLayers = previousExcludeLayers;

			Body.FeetCollider.enabled = MovementType == EMovementType.Normal;
		}
		public void SetRelationshipGroup(int value)
		{
			if (!IsAlive || RelationshipGroup == value)
				return;

			var previousRelationshipGroup = RelationshipGroup;
			RelationshipGroup = value;
			
			OnRelationshipGroupChangedEvent?.Invoke(this, previousRelationshipGroup, RelationshipGroup);
		}
		public virtual void SetMaxSpeed(float maximumSpeed)
		{
			MaximumSpeed = maximumSpeed;
		}

		public virtual void SelectSpell(SpellData data)
		{
			Spell?.Unselect();
			SpellRange = float.MaxValue;

			for (var i = 0; i < Spells.Count; i++)
			{
				var spell = Spells[i];
				if (spell.SpellData != data)
					continue;

				Spell = spell;
				Spell.Select();
				SpellRange = data.Range;
				
				break;
			}
		}
		public virtual bool HasSpell(SpellData data)
		{
			for (var i = 0; i < Spells.Count; i++)
			{
				if (Spells[i].SpellData != data)
					continue;

				return true;
			}
			
			return false;
		}
		public virtual void LearnSpell(SpellData data, bool autoSelect)
		{
			if (HasSpell(data))
				return;

			var type = data.Type == "" ? typeof(BaseSpell) : Type.GetType(data.Type);
			
			var spell = (ISpell)thisGo.AddComponent(type);
			spell.SpellData = data;
			spell.Owner = this;
			
			Spells.Add(spell);
			
			if (autoSelect)
				SelectSpell(data);
		}
		public virtual void ForgetSpell(SpellData data)
		{
			for (var i = Spells.Count - 1; i >= 0; i--)
			{
				var spell = Spells[i];
				if (spell.SpellData != data)
					continue;
				
				spell.Unselect();
				
				Destroy((Component)spell);
			}
		}
		public virtual void ForgetAllSpells()
		{
			for (var i = Spells.Count - 1; i >= 0; i--)
			{
				var spell = Spells[i];
				spell.Unselect();
				
				Destroy((Component)spell);
			}
		}
		
		public virtual bool HasWearable(WearableData data)
		{
			for (var i = 0; i < Wearables.Count; i++)
			{
				if (Wearables[i].WearableData != data)
					continue;

				return true;
			}
			
			return false;
		}
		public virtual void EquipWearable(IWearable wearable)
		{
			var data = wearable.WearableData;
			
			if (HasWearable(data))
				return;
			
			for (var i = Wearables.Count - 1; i >= 0; i--)
			{
				var innerData = Wearables[i].WearableData;
				if (innerData.WearableType != data.WearableType)
					continue;

				DropWearable(innerData);
			}
			
			wearable.Equip(this);
			
			Wearables.Add(wearable);
		}
		public virtual void DropWearable(WearableData data)
		{
			for (var i = Wearables.Count - 1; i >= 0; i--)
			{
				var wearable = Wearables[i];
				if (wearable.WearableData != data)
					continue;
				
				wearable.Drop();
				Wearables.RemoveAt(i);
				
				return;
			}
		}
		public virtual void DropAllWearables()
		{
			for (var i = Wearables.Count - 1; i >= 0; i--)
				DropWearable(Wearables[i].WearableData);
		}

		public virtual void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed, int relationshipGroup)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Characters);
				init = true;
			}
			
			if (IsAlive)
				return;

			Wearables = new List<IWearable>();
			Spells = new List<ISpell>();
			
			CurrentHealth = startingHealth;
			StartingHealth = startingHealth;
			OverloadHealth = overloadHealth;
			RegenerateHealth = regenerateHealth;

			CurrentMana = startingMana;
			StartingMana = startingMana;
			OverloadMana = overloadMana;
			RegenerateMana = regenerateMana;

			IsAlive = true;
			
			SetMaxSpeed(maximumSpeed);
			SetRelationshipGroup(relationshipGroup);
			
			SpellRange = float.MaxValue;
			
			OnSpawnEvent?.Invoke(this);
			
			regenerateLoop().Forget();
		}
		public virtual void Heal(float health, object source, bool clamp = false)
		{
			if (!IsAlive || health < 0)
				return;
			
			if (clamp)
			{
				if (CurrentHealth >= StartingHealth)
					return;

				if (CurrentHealth + health >= StartingHealth)
					health = StartingHealth - CurrentHealth;
			}
			
			CurrentHealth += health;
			OnHealEvent?.Invoke(this, health, source);
			
			if (CurrentHealth >= OverloadHealth)
				Kill(this);
		}
		public virtual void Damage(float damage, object source, EDamageType type)
		{
			if (!IsAlive || damage < 0 || IsInvulnerable)
				return;
			
			CurrentHealth -= damage;
			OnDamageEvent?.Invoke(this, damage, source, type);

			if (CurrentHealth > 0)
				return;
			
			Kill(source);
		}
		public virtual void GenerateMana(float mana, object source, bool clamp = false)
		{
			if (!IsAlive || mana < 0)
				return;

			if (clamp)
			{
				if (CurrentMana >= StartingMana)
					return;

				if (CurrentMana + mana >= StartingMana)
					mana = StartingMana - CurrentMana;
			}
			
			CurrentMana += mana;
			OnManaGenerateEvent?.Invoke(this, mana, source);
			
			if (CurrentMana >= OverloadMana)
				Kill(this);
		}
		public virtual void UseMana(float mana, object source)
		{
			if (!IsAlive || mana < 0 || IsPowerful)
				return;
			
			CurrentMana -= mana;
			OnManaUseEvent?.Invoke(this, mana, source);
		}

		public virtual void Kill(object source)
		{
			if (!IsAlive)
				return;
			
			SetMovementType(EMovementType.Normal);
			DropAllWearables();

			CurrentHealth = 0;
			CurrentMana = 0;
			IsAlive = false;
			
#if BODY_GIB
			Body.Rigidbody.constraints = RigidbodyConstraints.None;
			Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

			Body.Rigidbody.isKinematic = false;
			Body.Rigidbody.AddForce(Random.Range(-25f, 25f), 100f, Random.Range(-25f, 25f), ForceMode.Impulse);

			Body.BodyCollider.material = null;
			Body.FeetCollider.material = null;

			var ragdolls = World.World.Instance.Ragdolls;
			var length = Body.Gibs.Length;

			for (var i = 0; i < length; i++)
			{
				var isLast = i == length - 1;
				
				var gib = Body.Gibs[i];
				gib.enabled = true;

				var go = gib.gameObject;
				go.layer = 0;

				if (!isLast)
				{
					var coll = go.GetComponent<Collider>();
					coll.excludeLayers = 0;
					coll.material = null;
					coll.enabled = true;
				}

				var rb = isLast ? Body.Rigidbody : go.AddComponent<Rigidbody>();
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.automaticInertiaTensor = false;
				rb.excludeLayers = 0;
				rb.mass = 5;

				go.transform.SetParent(ragdolls);
			}
#else
			thisGo.SetActive(false);
#endif

			AIManager.Instance.AlivesColliderMap.Remove(Body.BodyCollider);
			
			OnDeathEvent?.Invoke(this, source);
		}

		public virtual bool IsGrounded()
		{
			return true;
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid regenerateLoop()
		{
			while (IsAlive)
			{
				await UniTask.WaitForSeconds(0.5f);

				GenerateMana(RegenerateMana, this, true);
				Heal(RegenerateHealth, this, true);
			}
		}
		
		#endregion
	}
}