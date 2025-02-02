#define BODY_GIB
//#define DEBUG_DAMAGE

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Enums;
using AI.Events;
using AI.Interfaces;
using Combat.Enums;
using Combat.Spells.Base;
using Combat.Spells.Interfaces;
using Combat.Structs;
using Combat.Wearables.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using ScriptableObjects;
using Tools;
using UI.Hotbar;
using UI.Spellbook;
using UnityEngine;
using Random = UnityEngine.Random;

namespace AI.Base
{
	public class BaseAlive : MonoBehaviour, IAlive
	{
		public static readonly OnRestoreHealthEvent OnRestoreHealthEvent = new ();
		public static readonly OnDamageEvent OnDamageEvent = new ();
		public static readonly OnRestoreManaEvent OnRestoreManaEvent = new ();
		public static readonly OnRestoreEnergyEvent OnRestoreEnergyEvent = new ();
		public static readonly OnTakeManaEvent OnTakeManaEvent = new ();
		public static readonly OnTakeEnergyEvent OnTakeEnergyEvent = new ();
		public static readonly OnDeathEvent OnDeathEvent = new ();
		public static readonly OnSpawnEvent OnSpawnEvent = new ();
		public static readonly OnRelationshipGroupChangedEvent OnRelationshipGroupChangedEvent = new ();
		public static readonly OnSpellSelectedEvent OnSpellSelectedEvent = new ();
		
		private LayerMask previousExcludeLayers;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		#region MonoBehaviour

		// todo: needs fixing, sometimes very wrong
		/*public void OnCollisionEnter(Collision coll)
		{
			if (!IsAlive)
				return;

			var velocity = coll.relativeVelocity.y - Body.FallMinimumVelocity;
			if (velocity < 0f)
				return;

			var damage = Mathf.FloorToInt(Body.FallDamageMultiplier * (velocity * velocity));
			Damage(damage, null, EElement.Unknown);
		}*/

		#endregion
		
		#region IAlive

		[field: SerializeField]
		public AliveData Data { get; private set; }

		[field: SerializeField]
		public Body Body { get; private set; }

		public Dictionary<EElement, SElementStat> DamageStats { get; private set; } = new ();
		public Dictionary<EElement, SElementStat> ProtectionStats { get; private set; } = new ();
		
		public List<IWearable> Wearables { get; private set; }
		public List<ISpell> Spells { get; private set; }

		public ISpell Spell { get; private set; }

		public virtual float CurrentSpeed { get; private set; }
		public float CurrentHealth { get; private set; }
		public float CurrentMana { get; private set; }
		public float CurrentEnergy { get; private set; }

		public EMovementType MovementType { get; private set; }
		public int RelationshipGroup { get; private set; }
		public float SpellRange { get; private set; }
		
		public float SlowAmount
		{
			get
			{
				var amount = 0f;

				foreach (var pair in SlowSources)
				{
					if (pair.Value <= amount)
						continue;
					
					amount = pair.Value;
				}
				
				return amount;
			}
		}
		public Dictionary<int, float> SlowSources { get; private set; } = new ();

		public bool Paralyzed => ParalyzeSources.Count > 0;
		public List<int> ParalyzeSources { get; private set; } = new ();
		
		public bool IsAlive { get; private set; }
		public bool IsInvulnerable { get; private set; }
		public bool IsPowerful { get; private set; }
		public virtual bool IsWalking { get; private set; }
		public bool IsCasting => Spell != null && Spell.IsCasting;

		public virtual void SetInvulnerable(bool value)
		{
			if (!IsAlive || IsInvulnerable == value)
				return;
			
			IsInvulnerable = value;
		}
		public virtual void SetPowerful(bool value)
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
		public virtual void SetRelationshipGroup(int value)
		{
			if (!IsAlive || RelationshipGroup == value)
				return;

			var previousRelationshipGroup = RelationshipGroup;
			RelationshipGroup = value;
			
			OnRelationshipGroupChangedEvent?.Invoke(this, previousRelationshipGroup, RelationshipGroup);
		}
		
		public virtual int AddSlowSource(float amount, float duration)
		{
			if (duration <= 0f)
				return -1;

			var id = Random.Range(0, int.MaxValue);
			addTemporarySlow(id, amount, duration).Forget();

			return id;
		}
		public virtual void AddSlowSource(int instanceID, float amount)
		{
			SlowSources[instanceID] = Mathf.Clamp01(amount);
		}
		public virtual void RemoveSlowSource(int instanceID)
		{
			SlowSources.Remove(instanceID);
		}
		public virtual void ClearSlowSources()
		{
			SlowSources.Clear();
		}
		
		public virtual int AddParalyzeSource(float duration)
		{
			if (duration <= 0f)
				return -1;

			var id = Random.Range(0, int.MaxValue);
			addTemporaryParalyze(id, duration).Forget();

			return id;
		}
		public virtual void AddParalyzeSource(int instanceID)
		{
			ParalyzeSources.AddUnique(instanceID);
			
			if (Paralyzed)
				Body.SetMalfunction(true);
		}
		public virtual void RemoveParalyzeSource(int instanceID)
		{
			ParalyzeSources.Remove(instanceID);
			
			if (!Paralyzed)
				Body.SetMalfunction(false);
		}
		public virtual void ClearParalyzeSources()
		{
			ParalyzeSources.Clear();
			
			Body.SetMalfunction(false);
		}

		public virtual int GetSpellIndex(SpellData data)
		{
			for (var i = 0; i < Spells.Count; i++)
				if (Spells[i].SpellData == data)
					return i;

			return -1;
		}
		public virtual void SetSpellIndex(SpellData data, int index)
		{
			var currentIndex = GetSpellIndex(data);
			if (currentIndex == -1 || currentIndex == index)
				return;

			var spell = Spells[currentIndex];
			Spells.RemoveAt(currentIndex);
			Spells.Insert(index, spell);
		}
		public virtual void SelectSpell(int index)
		{
			if (Spells.Count <= index)
				return;
			
			SelectSpell(Spells[index].SpellData);
		}
		public virtual void SelectSpell(SpellData data)
		{
			var previousSpell = Spell;
			
			if (Spell != null)
				Spell.Unselect();
			
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
			
			OnSpellSelectedEvent?.Invoke(this, previousSpell, Spell);
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
		public virtual void EquipWearable(WearableData data)
		{
			if (HasWearable(data))
				return;

			for (var i = Wearables.Count - 1; i >= 0; i--)
			{
				var innerData = Wearables[i].WearableData;
				if (innerData.WearableType != data.WearableType)
					continue;

				DropWearable(innerData);
			}

			var wearable = ObjectManager.Instance.CreateWearable(data, Vector3.zero, Vector3.zero);
			
			wearable.Equip(this);
			Wearables.Add(wearable);
			
			recalculateStats();
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
			
			recalculateStats();
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
			
			recalculateStats();
		}
		public virtual void DropAllWearables()
		{
			for (var i = Wearables.Count - 1; i >= 0; i--)
				DropWearable(Wearables[i].WearableData);
		}

		public virtual void Spawn(AliveData data, int relationshipGroup)
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

			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, $"{name} has spawned");

			Data = data;
			
			Wearables = new List<IWearable>();
			Spells = new List<ISpell>();
			
			Body.SetCoreGlow(EElement.Unknown);

			CurrentHealth = data.Health;
			CurrentMana = data.Mana;
			CurrentEnergy = data.Energy;
			
			SpellRange = float.MaxValue;
			IsAlive = true;
			
			SetRelationshipGroup(relationshipGroup);

			var wearables = data.Wearables;

			for (var i = 0; i < wearables.Count; i++)
				EquipWearable(wearables[i]);
			
			var spells = data.Spells;
			
			for (var i = 0; i < spells.Count; i++)
				LearnSpell(spells[i], i == 0);
			
			recalculateStats();
			regenerateLoop().Forget();
			
			OnSpawnEvent?.Invoke(this);
		}
		public virtual void Kill(object source)
		{
			if (!IsAlive)
				return;
			
			ConsoleManager.Instance.AddEntry(ConsoleManager.EConsoleEntryType.Info, $"{name} was killed by {source}");
			
			if (Spell != null)
				Spell.CancelCasting();
			
			SetMovementType(EMovementType.Normal);
			DropAllWearables();

			Body.SetCoreGlow(EElement.Unknown);
			Body.SetCoreCenter(false);

			Body.Malfunction.gameObject.SetActive(false);

			CurrentHealth = 0;
			CurrentMana = 0;
			CurrentEnergy = 0;
			IsAlive = false;
			
#if BODY_GIB
			Body.Rigidbody.constraints = RigidbodyConstraints.None;
			Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

			Body.Rigidbody.isKinematic = false;
			Body.Rigidbody.useGravity = true;
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
					var colliders = go.GetComponents<Collider>();
					for (var k = 0; k < colliders.Length; k++)
					{
						var coll = colliders[k];
						if (!coll.isTrigger)
						{
							coll.excludeLayers = 0;
							coll.material = null;
						}

						coll.enabled = true;
					}
				}

				var rb = isLast ? Body.Rigidbody : go.AddComponent<Rigidbody>();
				rb.interpolation = RigidbodyInterpolation.Interpolate;
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

		public virtual void RestoreHealth(float health, object source)
		{
			if (!IsAlive || health < 0)
				return;
			
			if (CurrentHealth >= Data.Health)
				return;

			if (CurrentHealth + health >= Data.Health)
				health = Data.Health - CurrentHealth;
			
			CurrentHealth += health;
			OnRestoreHealthEvent?.Invoke(this, health, source);
		}
		public virtual void RestoreMana(float mana, object source)
		{
			if (!IsAlive || mana < 0)
				return;

			if (CurrentMana >= Data.Mana)
				return;

			if (CurrentMana + mana >= Data.Mana)
				mana = Data.Mana - CurrentMana;
			
			CurrentMana += mana;
			OnRestoreManaEvent?.Invoke(this, mana, source);
		}
		public virtual void RestoreEnergy(float energy, object source)
		{
			if (!IsAlive || energy < 0)
				return;

			if (CurrentEnergy >= Data.Energy)
				return;

			if (CurrentEnergy + energy >= Data.Energy)
				energy = Data.Energy - CurrentEnergy;
			
			CurrentEnergy += energy;
			OnRestoreEnergyEvent?.Invoke(this, energy, source);
		}

		public virtual void Damage(float damage, object source, EElement type)
		{
			if (!IsAlive || IsInvulnerable)
				return;
#if DEBUG_DAMAGE
			var original = damage;
#endif
			// Use the attackers damage stats to increase the damage they deal to this alive
			if (source is IAlive alive && alive.DamageStats.TryGetValue(type, out var attackStat))
				attackStat.Add(ref damage);
#if DEBUG_DAMAGE
			var postDamage = damage;
#endif
			// Use this alives protection stats to reduce the damage they take from the attacker
			if (ProtectionStats.TryGetValue(type, out var protectionStat))
				protectionStat.Subtract(ref damage);
#if DEBUG_DAMAGE
			Debug.Log($"[{name}] Taking {type} damage from {source}. {original} -> {postDamage} -> {damage}");
#endif
			if (damage < 0)
				return;
			
			CurrentHealth -= damage;
			OnDamageEvent?.Invoke(this, damage, source, type);

			if (CurrentHealth > 0)
				return;
			
			Kill(source);
		}
		public virtual void TakeMana(float mana, object source)
		{
			if (!IsAlive || mana < 0 || IsPowerful)
				return;
			
			if (CurrentMana < 0f)
				return;
			
			if (CurrentMana - mana <= 0f)
				mana = 0f;
			
			CurrentMana -= mana;
			OnTakeManaEvent?.Invoke(this, mana, source);
		}
		public virtual void TakeEnergy(float energy, object source)
		{
			if (!IsAlive || energy < 0 || IsPowerful)
				return;
			
			if (CurrentEnergy < 0f)
				return;
			
			if (CurrentEnergy - energy <= 0f)
				energy = 0f;
			
			CurrentEnergy -= energy;
			OnTakeEnergyEvent?.Invoke(this, energy, source);
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
			while (true)
			{
				await UniTask.WaitForSeconds(0.5f);

				if (this == null || !IsAlive || !isActiveAndEnabled)
					return;
				
				RestoreEnergy(Data.RegenerateEnergy, this);
				RestoreMana(Data.RegenerateMana, this);
				RestoreHealth(Data.RegenerateHealth, this);
			}
		}

		private async UniTaskVoid addTemporarySlow(int id, float amount, float duration)
		{
			AddSlowSource(id, amount);

			await UniTask.WaitForSeconds(duration);
			
			if (this == null || !IsAlive || !isActiveAndEnabled)
				return;
			
			RemoveSlowSource(id);
		}
		
		private async UniTaskVoid addTemporaryParalyze(int id, float duration)
		{
			AddParalyzeSource(id);

			await UniTask.WaitForSeconds(duration);
			
			if (this == null || !IsAlive || !isActiveAndEnabled)
				return;
			
			RemoveParalyzeSource(id);
		}
		
		private void recalculateStats()
		{
			#region Clear current stats

			DamageStats.Clear();
			ProtectionStats.Clear();

			var values = Enum.GetValues(typeof(EElement));
			
			for (var i = 0; i < values.Length; i++)
			{
				var element = (EElement)i;
				
				DamageStats[element] = new SElementStat();
				ProtectionStats[element] = new SElementStat();
			}

			#endregion

			#region Base alive stats

			foreach (var pair in Data.DamageStats)
			{
				var stat = DamageStats[pair.Key];
				stat.AppendStat(pair.Value);

				DamageStats[pair.Key] = stat;
			}
				
			foreach (var pair in Data.ProtectionStats)
			{
				var stat = ProtectionStats[pair.Key];
				stat.AppendStat(pair.Value);
					
				ProtectionStats[pair.Key] = stat;
			}

			#endregion

			#region Wearable stats

			for (var i = 0; i < Wearables.Count; i++)
			{
				var wearable = Wearables[i];
				var data = wearable.WearableData;

				foreach (var pair in data.DamageStats)
				{
					var stat = DamageStats[pair.Key];
					stat.AppendStat(pair.Value);

					DamageStats[pair.Key] = stat;
				}
				
				foreach (var pair in data.ProtectionStats)
				{
					var stat = ProtectionStats[pair.Key];
					stat.AppendStat(pair.Value);
					
					ProtectionStats[pair.Key] = stat;
				}
			}
			
			#endregion
		}
		
		#endregion
	}
}