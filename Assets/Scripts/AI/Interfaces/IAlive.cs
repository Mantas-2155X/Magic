using System.Collections.Generic;
using AI.Enums;
using Combat.Spells.Interfaces;
using Combat.Wearables.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace AI.Interfaces
{
	public interface IAlive
	{
		public Body Body { get; }
		
		public List<IWearable> Wearables { get; }
		public List<ISpell> Spells { get; }

		public ISpell Spell { get; }

		public float CurrentSpeed { get; }
		public float MaximumSpeed { get; }

		public float CurrentHealth { get; }
		public float StartingHealth { get; }
		public float OverloadHealth { get; }
		public float RegenerateHealth { get; }

		public float CurrentMana { get; }
		public float StartingMana { get; }
		public float OverloadMana { get; }
		public float RegenerateMana { get; }

		public EMovementType MovementType { get; }
		public int RelationshipGroup { get; }
		
		public bool IsAlive { get; }
		public bool IsInvulnerable { get; }
		public bool IsPowerful { get; }
		public bool IsWalking { get; }
		
		public void SetInvulnerable(bool value);
		public void SetPowerful(bool value);
		public void SetMovementType(EMovementType value);
		public void SetRelationshipGroup(int value);
		public void SetMaxSpeed(float maximumSpeed);

		public void SelectSpell(SpellData data);
		public bool HasSpell(SpellData data);
		public void LearnSpell(SpellData data, bool autoSelect);
		public void ForgetSpell(SpellData data);
		public void ForgetAllSpells();
		
		public bool HasWearable(WearableData data);
		public void EquipWearable(IWearable wearable);
		public void DropWearable(WearableData data);
		public void DropAllWearables();

		public void Spawn(float startingHealth, float overloadHealth, float regenerateHealth, float startingMana, float overloadMana, float regenerateMana, float maximumSpeed, int relationshipGroup);
		public void Heal(float health, object source, bool clamp = false);
		public void Damage(float damage, object source);
		public void GenerateMana(float mana, object source, bool clamp = false);
		public void UseMana(float mana, object source);
		public void Kill(object source);
		
		public bool IsGrounded();

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}