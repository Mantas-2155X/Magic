using System.Collections.Generic;
using AI.Enums;
using AYellowpaper.SerializedCollections;
using Combat.Enums;
using Combat.Spells.Interfaces;
using Combat.Structs;
using Combat.Wearables.Interfaces;
using ScriptableObjects;
using UnityEngine;

namespace AI.Interfaces
{
	public interface IAlive
	{
		public AliveData Data { get; }

		public Body Body { get; }
		
		public Dictionary<EElement, SElementStat> DamageStats { get; }
		public Dictionary<EElement, SElementStat> ProtectionStats { get; }
		
		public List<IWearable> Wearables { get; }
		public List<ISpell> Spells { get; }

		public ISpell Spell { get; }

		public float CurrentSpeed { get; }
		public float CurrentHealth { get; }
		public float CurrentMana { get; }

		public EMovementType MovementType { get; }
		public int RelationshipGroup { get; }
		public float SpellRange { get; }
		public List<int> BindSources { get; }
		
		public bool IsAlive { get; }
		public bool IsInvulnerable { get; }
		public bool IsPowerful { get; }
		public bool IsWalking { get; }
		public bool IsBound { get; }
		public bool IsCasting { get; }
		
		public void SetInvulnerable(bool value);
		public void SetPowerful(bool value);
		public void SetMovementType(EMovementType value);
		public void SetRelationshipGroup(int value);
		
		public void AddBindSource(int instanceID);
		public void RemoveBindSource(int instanceID);
		public void ClearBindSources();

		public int GetSpellIndex(SpellData data);
		public void SetSpellIndex(SpellData data, int index);
		public void SelectSpell(int index);
		public void SelectSpell(SpellData data);
		public bool HasSpell(SpellData data);
		public void LearnSpell(SpellData data, bool autoSelect);
		public void ForgetSpell(SpellData data);
		public void ForgetAllSpells();
		
		public bool HasWearable(WearableData data);
		public void EquipWearable(WearableData data);
		public void EquipWearable(IWearable wearable);
		public void DropWearable(WearableData data);
		public void DropAllWearables();

		public void Spawn(AliveData data, int relationshipGroup);
		public void Kill(object source);

		public void RestoreHealth(float health, object source);
		public void RestoreMana(float mana, object source);
		
		public void Damage(float damage, object source, EElement type);
		public void TakeMana(float mana, object source);
		
		public bool IsGrounded();

		public GameObject GetGameObject();
		public Transform GetTransform();
	}
}