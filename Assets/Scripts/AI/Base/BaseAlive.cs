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
using Combat.Wearables.Base;
using Combat.Wearables.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using ScriptableObjects;
using State.Enums;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Serialization;

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
		
		private readonly List<Rigidbody> collidedRigidbodies = new ();
		
		private LayerMask previousExcludeLayers;

		private GameObject thisGo;
		private Transform thisTr;

		private bool init;
		
		#region Identify / SaveLoad
		
		public virtual bool ShouldSave => true;
		
		public virtual bool ShouldTransfer => true;
		
		public virtual bool ExternallySpawned { get; set; }

		public virtual string OriginalScene { get; set; }
		
		public virtual string TransferredScene { get; set; }
		
		public virtual ELoadType LoadType => ELoadType.Modify;
		
		public virtual ELoadTiming LoadTiming => ELoadTiming.Alives;

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}
		
		public virtual JObject GetCreation()
		{
			throw new NotImplementedException();
		}
		
		public virtual Dictionary<string, JObject> GetModifications()
		{
			var dict = new Dictionary<string, JObject>();
			dict[typeof(Transform).ToString()] = JObject.FromObject(new TransformState(thisTr));
			dict[typeof(Rigidbody).ToString()] = JObject.FromObject(new RigidbodyState(Body.Rigidbody));
			dict[typeof(BaseAlive).ToString()] = JObject.FromObject(new BaseAliveState(this));

			return dict;
		}

		public virtual void ApplyModifications(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState) && transformState != null)
				transformState.ToObject<TransformState>().Apply(thisTr);
			
			if (data.TryGetValue(typeof(Rigidbody).ToString(), out var rigidbodyState) && rigidbodyState != null)
				rigidbodyState.ToObject<RigidbodyState>().Apply(Body.Rigidbody);
			
			if (data.TryGetValue(typeof(BaseAlive).ToString(), out var baseAliveState) && baseAliveState != null)
				baseAliveState.ToObject<BaseAliveState>().Apply(this);
		}
		
		public virtual void Awake()
		{
			StateManager.Instance.RegisterObject(this);
		}

		public virtual void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		#region MonoBehaviour

		public virtual void Update()
		{
			if (PauseManager.IsPaused)
				return;

			if (!IsAlive)
				return;

			collidedRigidbodies.Clear();
		}

		public virtual void FixedUpdate()
		{
			if (PauseManager.IsPaused)
				return;

			if (!IsAlive)
				return;
			
			HandleGrab();
		}

		public void OnCollisionEnter(Collision collision)
		{
			if (!IsAlive)
				return;

			var rb = collision.rigidbody;
			if (rb == null || rb.linearVelocity.magnitude < 5f || collidedRigidbodies.Contains(rb) || rb.GetComponent<IObject>().IsNull())
				return;

			collidedRigidbodies.Add(rb);
			
			var magnitude = Mathf.Max(0f, (collision.relativeVelocity.sqrMagnitude * rb.mass) - Data.ImpactMinimumThreshold);
			if (magnitude == 0f)
				return;

			Damage(magnitude * Data.ImpactDamageScale, null, EElement.Unknown);
		}

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
					if (pair.Value.Item1 <= amount)
						continue;
					
					amount = pair.Value.Item1;
				}
				
				return amount;
			}
		}
		public Dictionary<string, Tuple<float, float, float>> SlowSources { get; private set; } = new ();

		public bool Paralyzed => ParalyzeSources.Count > 0;
		public Dictionary<string, Tuple<float, float>> ParalyzeSources { get; private set; } = new ();
		
		public IObject Grabbing { get; private set; }
		public Vector3? OriginalGrabSize { get; set; }

		public bool IsAlive { get; private set; }
		public bool IsInvulnerable { get; private set; }
		public bool IsPowerful { get; private set; }
		public virtual bool IsWalking { get; private set; }
		public bool IsCasting => Spell.NotNull() && Spell.IsCasting;

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
			Body.HitboxCollider.enabled = MovementType == EMovementType.Normal;
			
			if (MovementType != EMovementType.Normal)
				previousExcludeLayers = Body.Rigidbody.excludeLayers;
			else
				Body.Rigidbody.excludeLayers = previousExcludeLayers;

			if (Body.MovementCollider != null)
				Body.MovementCollider.enabled = MovementType == EMovementType.Normal;
		}
		public virtual void SetRelationshipGroup(int value)
		{
			if (!IsAlive || RelationshipGroup == value)
				return;

			var previousRelationshipGroup = RelationshipGroup;
			RelationshipGroup = value;
			
			OnRelationshipGroupChangedEvent?.Invoke(this, previousRelationshipGroup, RelationshipGroup);
		}
		
		public virtual void AddSlowSource(string objID, float amount, float duration)
		{
			if (duration <= 0f)
				return;

			if (Mathf.Approximately(duration, float.MaxValue))
			{
				SlowSources[objID] = new Tuple<float, float, float>(Mathf.Clamp01(amount), Time.time, duration);
			}
			else
			{
				addTemporarySlow(objID, amount, duration).Forget();
			}
		}
		public virtual void RemoveSlowSource(string objID)
		{
			SlowSources.Remove(objID);
		}
		public virtual void ClearSlowSources()
		{
			SlowSources.Clear();
		}
		
		public virtual void AddParalyzeSource(string objID, float duration)
		{
			if (duration <= 0)
				return;
			
			if (Mathf.Approximately(duration, float.MaxValue))
			{
				ParalyzeSources[objID] = new Tuple<float, float>(Time.time, float.MaxValue);
			
				if (Paralyzed)
				{
					if (Spell.NotNull())
						Spell.CancelCasting();
				
					Body.SetMalfunction(true);
				}
			}
			else
			{
				addTemporaryParalyze(objID, duration).Forget();
			}
		}
		public virtual void RemoveParalyzeSource(string objID)
		{
			ParalyzeSources.Remove(objID);
			
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
			
			if (Spell.NotNull())
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

			var type = data.Type == "" ? typeof(BaseSpell) : Type.GetType(data.Assembly == "" ? data.Type : $"{data.Type}, {data.Assembly}");
			if (type == null || !typeof(ISpell).IsAssignableFrom(type))
			{
				Debug.LogError($"[BaseAlive] Failed to learn spell {data.LocalizedName} as the custom type is not valid");
				return;
			}
			
			var spell = (ISpell)thisGo.AddComponent(type);
			spell.SpellData = data;
			spell.Owner = this;
			spell.ObjectID = Guid.NewGuid().ToString();
			
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
		
		public virtual int GetWearableIndex(WearableData data)
		{
			for (var i = 0; i < Wearables.Count; i++)
			{
				if (Wearables[i].WearableData != data)
					continue;

				return i;
			}
			
			return -1;
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
			
			OnSpawnEvent?.Invoke(this);
		}
		public virtual void Kill(object source, bool killSilently = false)
		{
			if (!IsAlive)
				return;
			
			Debug.Log($"[Alive {gameObject.name}] was killed by {source}");
			
			if (Spell.NotNull())
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

			Body.HitboxCollider.material = null;
			
			if (Body.MovementCollider != null)
				Body.MovementCollider.material = null;

			var objects = World.World.Instance.Objects;
			var length = Body.Gibs.Length;

			var objectLayer = LayerMask.NameToLayer("Object");
			var explodeList = new List<Rigidbody>();
			
			for (var i = 0; i < length; i++)
			{
				var gib = Body.Gibs[i];
				gib.enabled = true;
				gib.ObjectID = Guid.NewGuid().ToString();
				gib.ExternallySpawned = true;
				
				if (gib.Geometry != null)
					gib.Geometry.enabled = true;

				if (gib.DynamicObject != null)
					gib.DynamicObject.enabled = true;
				
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
				
				explodeList.Add(rb);

				gib.Rigidbody = rb;
				
				go.transform.SetParent(objects);
			}

			var bodyPos = thisTr.position;
			
			for (var i = 0; i < explodeList.Count; i++)
				explodeList[i].AddExplosionForce(350f, bodyPos, 2f);
			
			if (!killSilently)
			{
				if (Data.BreakAudio != null)
					AudioManager.Instance.PlayAtPoint(Data.BreakAudio, thisTr.position);

				if (SettingsManager.Instance.GetInt("graphics-modelquality") >= 2)
				{
					var ragdolls = World.World.Instance.Ragdolls;
					var scale = thisTr.localScale;
					
					if (Data.BrokenBodyPrefabReference.RuntimeKeyIsValid())
					{
						var broken = Addressables.InstantiateAsync(Data.BrokenBodyPrefabReference, ragdolls).WaitForCompletion();
					
						var brokenTr = broken.transform;
						brokenTr.position = thisTr.position;
						brokenTr.rotation = thisTr.rotation;
						brokenTr.localScale = scale;
					
						broken.SetActive(true);
					}

					if (Data.BrokenArmPrefabReference.RuntimeKeyIsValid())
					{
						for (var i = 0; i < Body.Arms.Length; i++)
						{
							var arm = Body.Arms[i];
					
							var broken = Addressables.InstantiateAsync(Data.BrokenArmPrefabReference, ragdolls).WaitForCompletion();
					
							var brokenTr = broken.transform;
							brokenTr.position = arm.position;
							brokenTr.rotation = arm.rotation;
							brokenTr.localScale = scale;
					
							broken.SetActive(true);
						}
					}

					if (Data.BrokenFootPrefabReference.RuntimeKeyIsValid())
					{
						for (var i = 0; i < Body.Feet.Length; i++)
						{
							var foot = Body.Feet[i];
					
							var broken = Addressables.InstantiateAsync(Data.BrokenFootPrefabReference, ragdolls).WaitForCompletion();
					
							var brokenTr = broken.transform;
							brokenTr.position = foot.position;
							brokenTr.rotation = foot.rotation;
							brokenTr.localScale = scale;
					
							broken.SetActive(true);
						}
					}
				}
			}

			if (this is not Player)
				StateManager.Instance.RegisterKilledAlive(ObjectID);
			
			AIManager.Instance.AlivesColliderMap.Remove(Body.HitboxCollider);
			
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
			
			if (Grabbing.NotNull())
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
			if (Grabbing.IsNull())
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
			if (Grabbing.IsNull())
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
			
			var portal = ObjectManager.Instance.GetData<ObjectData>("OBJECT_PORTAL_NAME");
			ObjectManager.Instance.CreateObject(portal, tr.position, Vector3.zero);
		}

		public virtual void HandleGrab()
		{
			if (Grabbing.IsNull())
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
			var coreUp = Body.Core.up;

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
				rb.MovePosition(corePos + coreUp);
				rb.MoveRotation(Body.Rigidbody.rotation);
				
				return;
			}
			
			if (Vector3.Distance(corePos, objPos) > data.GrabDropDistance)
			{
				ReleaseObject();
				return;
			}
			
			if (Vector3.Angle(objPos - corePos, coreUp) > data.GrabDropAngle)
			{
				ReleaseObject();
				return;
			}
			
			var linearVelocity = corePos + coreUp - objPos;
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

		private async UniTaskVoid addTemporarySlow(string id, float amount, float duration)
		{
			SlowSources[id] = new Tuple<float, float, float>(Mathf.Clamp01(amount), Time.time, duration);

			await UniTask.WaitForSeconds(duration);
			
			if (this == null || !IsAlive || !isActiveAndEnabled)
				return;
			
			RemoveSlowSource(id);
		}
		
		private async UniTaskVoid addTemporaryParalyze(string id, float duration)
		{
			ParalyzeSources[id] = new Tuple<float, float>(Time.time, duration);
			
			if (Paralyzed)
			{
				if (Spell.NotNull())
					Spell.CancelCasting();
				
				Body.SetMalfunction(true);
			}

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
		
		[JsonObject]
		public class BaseAliveState : IState
		{
			[JsonProperty]
			public Dictionary<string, BaseWearable.BaseWearableState> Wearables;
		
			[JsonProperty]
			public Dictionary<string, BaseSpell.BaseSpellState> Spells;
		
			[JsonProperty]
			public float CurrentHealth;
		
			[JsonProperty]
			public float CurrentMana;
		
			[JsonProperty]
			public float CurrentEnergy;
		
			[JsonProperty]
			public EMovementType MovementType;

			[JsonProperty]
			public int RelationshipGroup;
		
			[JsonProperty]
			public string Grabbing;
		
			[JsonProperty]
			public Vector3? OriginalGrabSize;

			[JsonProperty]
			public bool Alive;
		
			[JsonProperty]
			public bool Invulnerable;

			[JsonProperty]
			public bool Powerful;
		
			// objectid -> amount, remaining duration
			[JsonProperty]
			public Dictionary<string, Tuple<float, float>> SlowSources;
		
			// objectid -> remaining duration
			[JsonProperty]
			public Dictionary<string, float> ParalyzeSources;
			
			public BaseAliveState() { }
			
			public BaseAliveState(object obj)
			{
				Read(obj);
			}
			
			public void Read(object obj)
			{
				if (obj is not BaseAlive baseAlive)
					return;

				Wearables = new Dictionary<string, BaseWearable.BaseWearableState>();
			
				for (var i = 0; i < baseAlive.Wearables.Count; i++)
				{
					var wearable = baseAlive.Wearables[i];
					Wearables.Add(wearable.WearableData.Name, new BaseWearable.BaseWearableState(wearable));
				}

				Spells = new Dictionary<string, BaseSpell.BaseSpellState>();
			
				for (var i = 0; i < baseAlive.Spells.Count; i++)
				{
					var spell = baseAlive.Spells[i];
					Spells.Add(spell.SpellData.Name, new BaseSpell.BaseSpellState(spell));
				}
			
				CurrentHealth = baseAlive.CurrentHealth;
				CurrentMana = baseAlive.CurrentMana;
				CurrentEnergy = baseAlive.CurrentEnergy;
			
				MovementType = baseAlive.MovementType;
				RelationshipGroup = baseAlive.RelationshipGroup;

				if (baseAlive.Grabbing.NotNull())
				{
					Grabbing = baseAlive.Grabbing.ObjectID;
					OriginalGrabSize = baseAlive.OriginalGrabSize;
				}
			
				Alive = baseAlive.IsAlive;
				Invulnerable = baseAlive.IsInvulnerable;
				Powerful = baseAlive.IsPowerful;

				SlowSources = new Dictionary<string, Tuple<float, float>>();
				foreach (var pair in baseAlive.SlowSources)
				{
					Tuple<float, float> tuple;
				
					if (Mathf.Approximately(pair.Value.Item3, float.MaxValue))
						tuple = new Tuple<float, float>(pair.Value.Item1, float.MaxValue);
					else
						tuple = new Tuple<float, float>(pair.Value.Item1, (pair.Value.Item2 + pair.Value.Item3) - Time.time);

					SlowSources.Add(pair.Key, tuple);
				}

				ParalyzeSources = new Dictionary<string, float>();
				foreach (var pair in baseAlive.ParalyzeSources)
				{
					float duration;
				
					if (Mathf.Approximately(pair.Value.Item2, float.MaxValue))
						duration = float.MaxValue;
					else
						duration = (pair.Value.Item1 + pair.Value.Item2) - Time.time;

					ParalyzeSources.Add(pair.Key, duration);
				}
			}
			
			public void Apply(object obj)
			{
				if (obj is not BaseAlive baseAlive)
					return;

				baseAlive.RemoveAllWearables();

				foreach (var pair in Wearables)
				{
					var wearableState = pair.Value;

					var wearableData = ObjectManager.Instance.GetData<WearableData>(pair.Key);
					baseAlive.EquipWearable(wearableData);
					
					var wearableIndex = baseAlive.GetWearableIndex(wearableData);
					wearableState.Apply(baseAlive.Wearables[wearableIndex]);
				}
				
				baseAlive.ForgetAllSpells();
				
				foreach (var pair in Spells)
				{
					var spellState = pair.Value;
				
					var spellData = ObjectManager.Instance.GetData<SpellData>(pair.Key);
					baseAlive.LearnSpell(spellData, spellState.Selected);

					var spellIndex = baseAlive.GetSpellIndex(spellData);
					spellState.Apply(baseAlive.Spells[spellIndex]);
				}

				var addHealth = CurrentHealth - baseAlive.CurrentHealth;
				switch (addHealth)
				{
					case > 0:
						baseAlive.RestoreHealth(addHealth, null);
						break;
					case < 0:
						baseAlive.Damage(Mathf.Abs(addHealth), null, EElement.Unknown);
						break;
				}
				
				var addMana = CurrentMana - baseAlive.CurrentMana;
				switch (addMana)
				{
					case > 0:
						baseAlive.RestoreMana(addMana, null);
						break;
					case < 0:
						baseAlive.TakeMana(Mathf.Abs(addMana), null);
						break;
				}
				
				var addEnergy = CurrentEnergy - baseAlive.CurrentEnergy;
				switch (addEnergy)
				{
					case > 0:
						baseAlive.RestoreEnergy(addEnergy, null);
						break;
					case < 0:
						baseAlive.TakeEnergy(Mathf.Abs(addEnergy), null);
						break;
				}
				
				baseAlive.SetMovementType(MovementType);
				baseAlive.SetRelationshipGroup(RelationshipGroup);
				
				baseAlive.ReleaseObject();
				
				if (!string.IsNullOrEmpty(Grabbing))
				{
					var world = World.World.Instance;
					
					var components = world.Objects.GetComponentsInChildren<Component>(true);
					for (var i = 0; i < components.Length; i++)
					{
						var component = components[i];
						if (component is not IObject iObject)
							continue;
						
						if (iObject.ObjectID != Grabbing)
							continue;

						baseAlive.GrabObject(iObject);
						
						if (OriginalGrabSize != null)
						{
							iObject.GetTransform().localScale = OriginalGrabSize.Value;
							iObject.Rigidbody.isKinematic = false;

							baseAlive.ShrinkObject(true);
						}
						break;
					}
				}

				if (!Alive && baseAlive.IsAlive)
					baseAlive.Kill(null);

				baseAlive.SetInvulnerable(Invulnerable);
				baseAlive.SetPowerful(Powerful);

				foreach (var pair in SlowSources)
					baseAlive.AddSlowSource(pair.Key, pair.Value.Item1, pair.Value.Item2);
				
				foreach (var pair in ParalyzeSources)
					baseAlive.AddParalyzeSource(pair.Key, pair.Value);
			}
		}
	}
}