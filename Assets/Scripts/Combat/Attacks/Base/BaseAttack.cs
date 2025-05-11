using System.Collections.Generic;
using System.Runtime.CompilerServices;
using AI.Interfaces;
using Combat.Attacks.Interfaces;
using Combat.Projectiles.Interfaces;
using Combat.Spells.Interfaces;
using Cysharp.Threading.Tasks;
using Managers;
using Newtonsoft.Json.Linq;
using Objects.Interfaces;
using ScriptableObjects;
using State.Interfaces;
using State.States;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.Attacks.Base
{
	public class BaseAttack : MonoBehaviour, IAttack
	{
		[field: SerializeField]
		public AttackData AttackData { get; private set; }
		
		public bool ShouldSave => true;

		[FormerlySerializedAs("<ObjectID>k__BackingField")][SerializeField]
		private string objectID;
		public string ObjectID
		{
			get => objectID;
			set => objectID = StateManager.Instance.ChangeObjectID(this, value);
		}

		public IIdentifiable Source { get; private set; }

		[field: SerializeField]
		public ParticleSystem System { get; private set; }
		[field: SerializeField]
		public Collider[] Triggers { get; private set; }

		public IIdentifiable Target { get; set; }

		public readonly List<IAlive> TriggeredAlives = new ();
		public readonly List<IAlive> CurrentAlives = new ();
		
		public readonly List<IObject> TriggeredObjects = new ();
		public readonly List<IObject> CurrentObjects = new ();
		
		public float CreatedTime { get; private set; }

		private GameObject thisGo;
		private Transform thisTr;

		private IAlive owner;
		
		private bool init;
		
		#region Identify / SaveLoad

		public virtual Dictionary<string, JObject> Save()
		{
			var dict = new Dictionary<string, JObject>();

			var transformState = TransformState.Read(thisTr);
			if (transformState != null)
				dict[typeof(Transform).ToString()] = JObject.FromObject(transformState);
			
			var baseAttackState = BaseAttackState.Read(this);
			if (baseAttackState != null)
				dict[typeof(BaseAttack).ToString()] = JObject.FromObject(baseAttackState);

			return dict;
		}

		public virtual void Load(Dictionary<string, JObject> data)
		{
			if (data.TryGetValue(typeof(Transform).ToString(), out var transformState))
				TransformState.Apply(thisTr, transformState.ToObject<TransformState>());
			
			if (data.TryGetValue(typeof(BaseAttack).ToString(), out var baseAttackState))
				BaseAttackState.Apply(this, baseAttackState.ToObject<BaseAttackState>());
		}

		public virtual void SetState(List<string> triggeredAlivesIDs, List<string> currentAlivesIDs, List<string> triggeredObjectIDs, List<string> currentObjectIDs)
		{
			var stateManager = StateManager.Instance;

			for (var i = 0; i < triggeredAlivesIDs.Count; i++)
			{
				var identifiable = stateManager.GetRegisteredObject(triggeredAlivesIDs[i]);
				if (identifiable.IsNull() || identifiable is not IAlive alive)
					continue;
				
				TriggeredAlives.Add(alive);
			}
			
			for (var i = 0; i < currentAlivesIDs.Count; i++)
			{
				var identifiable = stateManager.GetRegisteredObject(currentAlivesIDs[i]);
				if (identifiable.IsNull() || identifiable is not IAlive alive)
					continue;
				
				CurrentAlives.Add(alive);
			}
			
			for (var i = 0; i < triggeredObjectIDs.Count; i++)
			{
				var identifiable = stateManager.GetRegisteredObject(triggeredObjectIDs[i]);
				if (identifiable.IsNull() || identifiable is not IObject obj)
					continue;
				
				TriggeredObjects.Add(obj);
			}
			
			for (var i = 0; i < currentObjectIDs.Count; i++)
			{
				var identifiable = stateManager.GetRegisteredObject(currentObjectIDs[i]);
				if (identifiable.IsNull() || identifiable is not IObject obj)
					continue;
				
				CurrentObjects.Add(obj);
			}
		}

		public void Awake()
		{
			StateManager.Instance.RegisterObject(this);
		}

		public void OnDestroy()
		{
			StateManager.Instance.UnregisterObject(this);
		}
		
		#endregion
		
		public virtual void Spawn(IIdentifiable source, Vector3 position, Quaternion angles, IIdentifiable attach, float elapsedTime = 0f)
		{
			if (!init)
			{
				thisGo = gameObject;
				thisTr = thisGo.transform;
				thisTr.SetParent(World.World.Instance.Attacks);
				init = true;
			}

			if (AttackData.DropToGround && Physics.Raycast(position, Vector3.down, out var hit, float.MaxValue, ~LayerMaskTools.GetMaskWithAlives()))
				position = hit.point;
			
			Source = source;
			
			owner = null;
			owner = GetAlive();

			Target = AttackData.AttachToTarget ? attach : null;
			CreatedTime = Time.time;

			if (AttackData.FollowCaster && owner.NotNull())
				Target = owner;
			
			if (Target.IsNull())
			{
				thisTr.position = position + Vector3.up * 0.1f;
				thisTr.rotation = angles;
			}
			else
			{
				FollowTarget();
			}
			
			if (Triggers != null)
			{
				for (var i = 0; i < Triggers.Length; i++)
					Triggers[i].enabled = false;
				
				trigger(elapsedTime).Forget();
			}

			thisGo.SetActive(true);
			
			if (System != null)
			{
				if (elapsedTime > 0)
					System.Simulate(elapsedTime, true);
				
				System.Play(true);
			}
		}
		
		public void Update()
		{
			if (PauseManager.IsPaused)
				return;
			
			FollowTarget();
		}

		public virtual void OnDisable()
		{
			ObjectID = "";
			PoolingManager.Instance.Add(AttackData, thisGo);
		}

		public void OnParticleSystemStopped()
		{
			ObjectID = "";
			PoolingManager.Instance.Add(AttackData, thisGo);
		}

		public virtual void OnTriggerEnter(Collider other)
		{
			if (AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
			{
				if (AttackData.IgnoreCaster && alive == GetAlive())
					return;
			
				if (!TriggeredAlives.Contains(alive))
				{
					for (var i = 0; i < Triggers.Length; i++)
					{
						if (Triggers[i].bounds.Intersects(other.bounds))
							continue;

						return;
					}
				
					TriggeredAlives.Add(alive);
				
					if (AttackData.Damage != 0f)
						alive.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
				
					alive.AddSlowSource(ObjectID, AttackData.Slow.Amount, AttackData.Slow.Duration);
					alive.AddParalyzeSource(ObjectID, AttackData.Paralyze.Duration);
				}
			
				CurrentAlives.Add(alive);
			}
			else if (other.TryGetComponent<IObject>(out var obj))
			{
				if (!TriggeredObjects.Contains(obj))
				{
					for (var i = 0; i < Triggers.Length; i++)
					{
						if (Triggers[i].bounds.Intersects(other.bounds))
							continue;

						return;
					}
				
					TriggeredObjects.Add(obj);
				
					if (AttackData.Damage != 0f)
						obj.Damage(AttackData.Damage, GetAlive(), AttackData.Element);
				}
			
				CurrentObjects.Add(obj);
			}
		}
		
		public virtual void OnTriggerExit(Collider other)
		{
			if (AIManager.Instance.AlivesColliderMap.TryGetValue(other, out var alive))
				CurrentAlives.Remove(alive);
			else if (other.TryGetComponent<IObject>(out var obj))
				CurrentObjects.Remove(obj);
		}

		public virtual void OnTriggersEnabled()
		{
			TriggeredAlives.Clear();
			CurrentAlives.Clear();
			
			TriggeredObjects.Clear();
			CurrentObjects.Clear();

			for (var i = 0; i < Triggers.Length; i++)
				Triggers[i].enabled = true;
		}

		public virtual void OnTriggersDisabled()
		{
			for (var i = 0; i < Triggers.Length; i++)
				Triggers[i].enabled = false;
		}
		
		public void FollowTarget()
		{
			if (Target.IsNull())
				return;
			
			var targetTr = Target.GetTransform();
			var scale = targetTr.localScale.y;
			
			thisTr.position = targetTr.position + -targetTr.up * (0.95f * scale) + (AttackData.AttachOffset * scale);
		}
		
		public IAlive GetAlive()
		{
			if (Source.IsNull())
				return null;

			if (owner.NotNull())
				return owner;
			
			switch (Source)
			{
				case IAlive alive:
					return alive;
				case ISpell spell:
					return spell.Owner;
				case IAttack attack:
					return attack.GetAlive();
				case IProjectile projectile:
					return projectile.GetAlive();
				default:
					return null;
			}
		}
		
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public GameObject GetGameObject() => thisGo;
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Transform GetTransform() => thisTr;
		
		private async UniTaskVoid trigger(float elapsedTime)
		{
			if (elapsedTime < AttackData.EnableTriggerAfter)
			{
				await UniTask.WaitForSeconds(AttackData.EnableTriggerAfter - elapsedTime);
				
				if (this == null || !isActiveAndEnabled)
					return;
			}

			OnTriggersEnabled();
			
			elapsedTime -= AttackData.EnableTriggerAfter;
			
			if (elapsedTime < 0f)
				elapsedTime = 0f;

			if (elapsedTime < AttackData.DisableTriggerAfter)
			{
				await UniTask.WaitForSeconds(AttackData.DisableTriggerAfter - elapsedTime);
			
				if (this == null || !isActiveAndEnabled)
					return;
			}
			
			OnTriggersDisabled();
		}
	}
}