using System;
using System.Collections.Generic;
using AI.Enums;
using Combat.Enums;
using Combat.Spells.Interfaces;
using Combat.Structs;
using Combat.Wearables.Interfaces;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using UnityEngine;

namespace AI.Interfaces
{
	public interface IAlive : ISaveable
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
		public float CurrentEnergy { get; }

		public EMovementType MovementType { get; }
		public int RelationshipGroup { get; }
		public float SpellRange { get; }
		
		public float SlowAmount { get; }
		public Dictionary<string, Tuple<float, float, float>> SlowSources { get; }
		
		public bool Paralyzed { get; }
		public Dictionary<string, Tuple<float, float>> ParalyzeSources { get; }
		
		public IObject Grabbing { get; }
		public Vector3? OriginalGrabSize { get; }
		
		public bool IsAlive { get; }
		public bool IsInvulnerable { get; }
		public bool IsPowerful { get; }
		public bool IsWalking { get; }
		public bool IsCasting { get; }
		
		public void SetInvulnerable(bool value);
		public void SetPowerful(bool value);
		public void SetMovementType(EMovementType value);
		public void SetRelationshipGroup(int value);
		
		public void AddSlowSource(string objID, float amount, float duration);
		public void RemoveSlowSource(string objID);
		public void ClearSlowSources();
		
		public void AddParalyzeSource(string objID, float duration);
		public void RemoveParalyzeSource(string objID);
		public void ClearParalyzeSources();

		public int GetSpellIndex(SpellData data);
		public void SetSpellIndex(SpellData data, int index);
		public void SelectSpell(int index);
		public void SelectSpell(SpellData data);
		public bool HasSpell(SpellData data);
		public void LearnSpell(SpellData data, bool autoSelect);
		public void ForgetSpell(SpellData data);
		public void ForgetAllSpells();
		
		public int GetWearableIndex(WearableData data);
		public bool HasWearable(WearableData data);
		public void EquipWearable(WearableData data);
		public void EquipWearable(IWearable wearable);
		public void DropWearable(WearableData data);
		public void RemoveWearable(WearableData data);
		public void DropAllWearables();
		public void RemoveAllWearables();

		public void Spawn(AliveData data, int relationshipGroup);
		public void Kill(object source, bool killSilently = false);

		public void RestoreHealth(float health, object source);
		public void RestoreMana(float mana, object source);
		public void RestoreEnergy(float energy, object source);
		
		public void Damage(float damage, object source, EElement type);
		public void TakeMana(float mana, object source);
		public void TakeEnergy(float energy, object source);
		
		public bool IsGrounded();

		public void GrabObject(IObject obj);
		public void ReleaseObject();
		public void ShrinkObject(bool state);
		public void HandleGrab();
	}
}