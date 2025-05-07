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
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using ScriptableObjects;
using State.States;
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

		public virtual void FixedUpdate()
		{
			if (!IsAlive)
				return;
			
			HandleGrab();
		}

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
		public string ObjectID { get; set; }

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
		
		public IObject Grabbing { get; private set; }
		public Vector3? OriginalGrabSize { get; set; }

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

			if (Body.FeetCollider != null)
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
			{
				if (Spell != null)
					Spell.CancelCasting();
				
				Body.SetMalfunction(true);
			}
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
				Spells.RemoveAt(i);
				
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
			
			Spells.Clear();
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
		public virtual void RemoveWearable(WearableData data)
		{
			for (var i = Wearables.Count - 1; i >= 0; i--)
			{
				var wearable = Wearables[i];
				if (wearable.WearableData != data)
					continue;
				
				Destroy(wearable.GetGameObject());
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
		public virtual void RemoveAllWearables()
		{
			for (var i = Wearables.Count - 1; i >= 0; i--)
				RemoveWearable(Wearables[i].WearableData);
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

			Debug.Log($"[Alive {gameObject.name}] has spawned");

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
			
			StateManager.Instance.KilledAlives.Remove(ObjectID);
			
			OnSpawnEvent?.Invoke(this);
		}
		public virtual void Kill(object source, bool killSilently = false)
		{
			if (!IsAlive)
				return;
			
			Debug.Log($"[Alive {gameObject.name}] was killed by {source}");
			
			if (Spell != null)
				Spell.CancelCasting();
			
			SetMovementType(EMovementType.Normal);
			DropAllWearables();
			ReleaseObject();

			Body.SetCoreGlow(EElement.Unknown);
			Body.SetCoreCenter(false);

			Body.Malfunction.gameObject.SetActive(false);

			CurrentHealth = 0;
			CurrentMana = 0;
			CurrentEnergy = 0;
			IsAlive = false;
			
			Body.Rigidbody.constraints = RigidbodyConstraints.None;
			Body.Rigidbody.collisionDetectionMode = CollisionDetectionMode.Discrete;

			Body.Rigidbody.isKinematic = false;
			Body.Rigidbody.useGravity = true;

			Body.BodyCollider.material = null;
			
			if (Body.FeetCollider != null)
				Body.FeetCollider.material = null;

			var ragdolls = World.World.Instance.Ragdolls;
			var length = Body.Gibs.Length;

			var objectLayer = LayerMask.NameToLayer("Object");
			var force = -thisTr.forward * 0.25f;
			
			for (var i = 0; i < length; i++)
			{
				var gib = Body.Gibs[i];
				gib.enabled = true;
				gib.ObjectID = Guid.NewGuid().ToString();
				
				var go = gib.gameObject;
				go.layer = objectLayer;

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

				var rb = go.AddComponent<Rigidbody>();
				rb.interpolation = RigidbodyInterpolation.Interpolate;
				rb.excludeLayers = 0;
				rb.mass = 5;

				gib.Rigidbody = rb;
				
				rb.AddForce(force, ForceMode.VelocityChange);
					
				go.transform.SetParent(ragdolls);
			}
			
			if (!killSilently && SettingsManager.Instance.GetBool("graphics-shatterobjects") == true)
			{
				var scale = thisTr.localScale;
					
				if (Data.BrokenBodyPrefab != null)
				{
					var broken = Instantiate(Data.BrokenBodyPrefab, ragdolls);
					
					var brokenTr = broken.transform;
					brokenTr.position = thisTr.position;
					brokenTr.rotation = thisTr.rotation;
					brokenTr.localScale = scale;
					
					broken.SetActive(true);
				}

				if (Data.BrokenArmPrefab != null)
				{
					for (var i = 0; i < Body.Arms.Length; i++)
					{
						var arm = Body.Arms[i];
					
						var broken = Instantiate(Data.BrokenArmPrefab, ragdolls);
					
						var brokenTr = broken.transform;
						brokenTr.position = arm.position;
						brokenTr.rotation = arm.rotation;
						brokenTr.localScale = scale;
					
						broken.SetActive(true);
					}
				}

				if (Data.BrokenFootPrefab != null)
				{
					for (var i = 0; i < Body.Feet.Length; i++)
					{
						var foot = Body.Feet[i];
					
						var broken = Instantiate(Data.BrokenFootPrefab, ragdolls);
					
						var brokenTr = broken.transform;
						brokenTr.position = foot.position;
						brokenTr.rotation = foot.rotation;
						brokenTr.localScale = scale;
					
						broken.SetActive(true);
					}
				}
			}

			StateManager.Instance.KilledAlives.AddUnique(ObjectID);
			AIManager.Instance.AlivesColliderMap.Remove(Body.BodyCollider);
			
			OnDeathEvent?.Invoke(this, source);
			
			Destroy(thisGo);
		}

		public virtual void RestoreHealth(float health, object source)
		{
			if (!IsAlive || health < 0)
				return;
			
			if (CurrentHealth >= Data.Health)
				return;

			if (CurrentHealth + health >= Data.Health)
				health = Data.Health - CurrentHealth;
			
			if (health == 0)
				return;
			
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
			
			if (mana == 0)
				return;
			
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
			
			if (energy == 0)
				return;
			
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
			Debug.Log($"[Alive {gameObject.name}] Taking {type} damage from {source}. {original} -> {postDamage} -> {damage}");
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

		public virtual void GrabObject(IObject obj)
		{
			if (!Data.CanGrab)
				return;
			
			if (Grabbing != null)
			{
				ReleaseObject();
				return;
			}

			var alives = AIManager.Instance.AlivesColliderMap;
			foreach (var pair in alives)
			{
				if (!pair.Value.IsAlive || pair.Value.Grabbing != obj)
					continue;

				pair.Value.ReleaseObject();
				break;
			}
			
			Grabbing = obj;

			var rb = obj.Rigidbody;
			rb.useGravity = false;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			
			RenderManager.Instance.OutlineFeature.AddRenderers(rb.GetComponentsInChildren<Renderer>());
		}

		public virtual void ReleaseObject()
		{
			if (Grabbing == null)
				return;
			
			ShrinkObject(false);

			var rb = Grabbing.Rigidbody;
			rb.useGravity = true;
			rb.linearVelocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;
			
			Grabbing = null;
			
			RenderManager.Instance.OutlineFeature.RemoveRenderers(rb.GetComponentsInChildren<Renderer>());
		}

		public virtual void ShrinkObject(bool state)
		{
			if (Grabbing == null)
				return;

			var tr = Grabbing.GetTransform();
			var rb = Grabbing.Rigidbody;
			
			if (state)
			{
				// Already shrinked
				if (OriginalGrabSize != null)
					return;
				
				OriginalGrabSize = tr.localScale;
				rb.isKinematic = true;
				tr.localScale = Vector3.zero;
			}
			else
			{
				// Already expanded
				if (OriginalGrabSize == null)
					return;
				
				tr.localScale = OriginalGrabSize.Value;
				rb.isKinematic = false;
				OriginalGrabSize = null;
			}
			
			var portal = ObjectManager.Instance.GetObject("OBJECT_PORTAL_NAME");
			ObjectManager.Instance.CreateObject(portal, tr.position, Vector3.zero);
		}

		public virtual void HandleGrab()
		{
			if (Grabbing == null)
				return;

			var data = Data;
			var grabEnergy = (OriginalGrabSize != null ? data.GrabShinkedEnergy : data.GrabEnergy) * Time.deltaTime;
			
			if (CurrentEnergy >= grabEnergy)
			{
				TakeEnergy(grabEnergy, this);
			}
			else
			{
				ReleaseObject();
				return;
			}

			var corePos = Body.Core.position;
			var coreForward = Body.Core.forward;

			switch (this)
			{
				case Player player:
					corePos.y = (player.CameraTr.position + player.CameraTr.forward).y + data.GrabVerticalOffset;
					break;
				case NPC:
					corePos.y += data.GrabVerticalOffset;
					break;
				default:
					throw new NotImplementedException();
			}
			
			var rb = Grabbing.Rigidbody;
			var objPos = rb.position;

			// Shrinking makes it kinematic, no velocities so use MoveX and ignore distance and angle checks
			if (OriginalGrabSize != null)
			{
				rb.MovePosition(corePos + coreForward);
				rb.MoveRotation(Body.Rigidbody.rotation);
				
				return;
			}
			
			if (Vector3.Distance(corePos, objPos) > data.GrabDropDistance)
			{
				ReleaseObject();
				return;
			}
			
			if (Vector3.Angle(objPos - corePos, coreForward) > data.GrabDropAngle)
			{
				ReleaseObject();
				return;
			}
			
			var linearVelocity = corePos + coreForward - objPos;
			rb.linearVelocity = linearVelocity * data.GrabPositionSpeed;
			
			var deltaRotation = Body.Rigidbody.rotation * Quaternion.Inverse(rb.rotation);
			deltaRotation.ToAngleAxis(out var angle, out var axis);
			
			if (angle > 180f)
				angle -= 360f;

			if (Mathf.Approximately(angle, 0)) 
			{
				rb.angularVelocity = Vector3.zero;
				return;
			}
			
			angle *= Mathf.Deg2Rad;

			var angularVelocity = axis * angle;
			rb.angularVelocity = angularVelocity * data.GrabRotationSpeed;
		}
		
		public virtual bool IsGrounded()
		{
			return true;
		}
		
		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var transformState = TransformState.Read(thisTr);
			if (transformState != null)
				dict[typeof(Transform).ToString()] = JObject.FromObject(transformState);

			var rigidbodyState = RigidbodyState.Read(Body.Rigidbody);
			if (rigidbodyState != null)
				dict[typeof(Rigidbody).ToString()] = JObject.FromObject(rigidbodyState);

			var baseAliveState = BaseAliveState.Read(this);
			if (baseAliveState != null)
				dict[typeof(BaseAlive).ToString()] = JObject.FromObject(baseAliveState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState))
				TransformState.Apply(thisTr, transformState.ToObject<TransformState>());
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState))
				RigidbodyState.Apply(Body.Rigidbody, rigidbodyState.ToObject<RigidbodyState>());
			
			if (data.TryGetValue(typeof(BaseAlive).ToString(), out var baseAliveState))
				BaseAliveState.Apply(this, baseAliveState.ToObject<BaseAliveState>());
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid regenerateLoop()
		{
			var regenerateEvery = 0.2f;
			
			while (true)
			{
				await UniTask.WaitForSeconds(regenerateEvery);

				if (this == null || !IsAlive || !isActiveAndEnabled)
					return;
				
				RestoreEnergy(Data.RegenerateEnergy * regenerateEvery, this);
				RestoreMana(Data.RegenerateMana * regenerateEvery, this);
				RestoreHealth(Data.RegenerateHealth * regenerateEvery, this);
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